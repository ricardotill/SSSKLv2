const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const sslDir = path.join(__dirname, 'ssl');
const certFile = path.join(sslDir, 'localhost.crt');
const keyFile = path.join(sslDir, 'localhost.key');
const pfxFile = path.join(sslDir, 'localhost.pfx');
const password = 'password';

// Ensure SSL directory exists
if (!fs.existsSync(sslDir)) {
    fs.mkdirSync(sslDir);
}

// Check if certificates already exist
if (fs.existsSync(certFile) && fs.existsSync(keyFile)) {
    console.log('SSL certificates already exist. Skipping generation.');
    process.exit(0);
}

console.log('Generating trusted SSL certificates for local development...');

try {
    // 1. Export the .NET development certificate to PFX
    // We try 'dotnet' first, then the common macOS path
    let dotnetCmd = 'dotnet';
    try {
        execSync('which dotnet', { stdio: 'ignore' });
    } catch (e) {
        if (fs.existsSync('/usr/local/share/dotnet/dotnet')) {
            dotnetCmd = '/usr/local/share/dotnet/dotnet';
        }
    }

    console.log(`Using dotnet from: ${dotnetCmd}`);
    execSync(`${dotnetCmd} dev-certs https -ep "${pfxFile}" -p ${password}`, { stdio: 'inherit' });

    // 2. Extract the private key using openssl
    console.log('Extracting private key...');
    execSync(`openssl pkcs12 -in "${pfxFile}" -nocerts -out "${keyFile}" -nodes -passin pass:${password}`, { stdio: 'inherit' });

    // 3. Extract the certificate using openssl
    console.log('Extracting certificate...');
    execSync(`openssl pkcs12 -in "${pfxFile}" -clcerts -nokeys -out "${certFile}" -passin pass:${password}`, { stdio: 'inherit' });

    // 4. Cleanup PFX file
    if (fs.existsSync(pfxFile)) {
        fs.unlinkSync(pfxFile);
    }

    console.log('SSL certificates generated successfully in Frontend/ssl/');
} catch (error) {
    console.error('Failed to generate SSL certificates:', error.message);
    process.exit(1);
}
