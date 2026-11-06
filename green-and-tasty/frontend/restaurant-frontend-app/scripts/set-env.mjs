/**
 * Generates src/environments/environment.production.ts from the BACKEND_URL env var.
 * Runs automatically before `npm run build` (npm "prebuild" hook).
 *
 * Usage:
 *   BACKEND_URL=https://xxxx.execute-api.eu-west-1.amazonaws.com/develop npm run build
 *
 * The generated file is git-ignored on purpose: the API URL is environment
 * configuration, not source code.
 */
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';

const OUT_FILE = resolve('src/environments/environment.production.ts');
const backendUrl = process.env.BACKEND_URL?.trim() || 'https://epam-task-09.onrender.com';

if (!/^https?:\/\//.test(backendUrl)) {
  console.error(`BACKEND_URL must start with http:// or https:// (got: ${backendUrl})`);
  process.exit(1);
}

const contents = `// GENERATED FILE — do not edit, do not commit.
// Produced by scripts/set-env.mjs from the BACKEND_URL environment variable.
export const environment = {
  backendUrl: '${backendUrl.replace(/\/$/, '')}',
};
`;

mkdirSync(dirname(OUT_FILE), { recursive: true });
writeFileSync(OUT_FILE, contents, 'utf8');
console.log(`environment.production.ts generated with backendUrl = ${backendUrl}`);
