/* eslint-disable */
const { env } = require('process');

const target = env.services__sssklv2__https__0 || env.services__sssklv2__http__0 || 'https://localhost:7193';

console.log(`Proxying /api to ${target}`);

const PROXY_CONFIG = {
  "/api": {
    "target": target,
    "secure": false,
    "changeOrigin": true
  },
  "/hubs": {
    "target": target,
    "secure": false,
    "changeOrigin": true,
    "ws": true
  }
};

module.exports = PROXY_CONFIG;
