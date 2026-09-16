/**
 * Converts a File/Blob to a base64 string (without the data: URL prefix).
 *
 * Multipart/form-data uploads are unreliable in iOS PWAs: when the app is
 * installed to the home screen with an active service worker, Safari can
 * strip the body from POST requests carrying a File/Blob part. Sending the
 * file as a fully-buffered base64 string in a JSON body avoids that bug.
 */
export function fileToBase64(file: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      const commaIndex = result.indexOf(',');
      resolve(commaIndex >= 0 ? result.slice(commaIndex + 1) : result);
    };
    reader.onerror = () => reject(reader.error ?? new Error('Failed to read file'));
    reader.readAsDataURL(file);
  });
}
