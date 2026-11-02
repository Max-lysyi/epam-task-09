import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const distDir = path.resolve(__dirname, '../dist/restaurant-frontend-app/browser');
const port = 4200;

const mimeTypes = {
  '.html': 'text/html',
  '.js': 'text/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.gif': 'image/gif',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
};

const server = http.createServer((req, res) => {
  const reqUrl = new URL(req.url, `http://localhost:${port}`);
  const reqPath = decodeURI(reqUrl.pathname);
  let filePath = path.join(distDir, reqPath);

  if (fs.existsSync(filePath) && fs.statSync(filePath).isFile()) {
    const ext = path.extname(filePath);
    res.writeHead(200, {
      'Content-Type': mimeTypes[ext] || 'application/octet-stream',
      'Access-Control-Allow-Origin': '*'
    });
    if (ext === '.js' || ext === '.html') {
      let content = fs.readFileSync(filePath, 'utf-8');
      const backendUrl = process.env.BACKEND_URL || 'http://localhost:5023';
      content = content.replaceAll('https://example.com', backendUrl);
      return res.end(content);
    }
    return fs.createReadStream(filePath).pipe(res);
  }

  // SPA fallback to index.html
  const indexPath = path.join(distDir, 'index.html');
  if (fs.existsSync(indexPath)) {
    res.writeHead(200, {
      'Content-Type': 'text/html',
      'Access-Control-Allow-Origin': '*'
    });
    let html = fs.readFileSync(indexPath, 'utf-8');
    const backendUrl = process.env.BACKEND_URL || 'http://localhost:5023';
    html = html.replaceAll('https://example.com', backendUrl);
    return res.end(html);
  } else {
    res.writeHead(404, { 'Content-Type': 'text/plain' });
    return res.end('Dist index.html not found');
  }
});

server.listen(port, () => {
  console.log(`Frontend SPA server running on http://localhost:${port}`);
});

