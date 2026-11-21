// Walidacja fixtures i przykładów kontraktem. Uruchom: npm --prefix contracts run validate
// Opcjonalnie: CASES_PRIVATE_DIR=<katalog> — sprawdza też prywatne osoby spraw ZUS (pliki poza repo).
import { readFileSync, readdirSync, existsSync } from "node:fs";
import { join, dirname, basename, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";
import yaml from "js-yaml";

const contracts = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const root = resolve(contracts, "..");
const fixtures = join(root, "fixtures");
const BASE = "https://babyradnik.local/contracts/";

const readJson = (p) => JSON.parse(readFileSync(p, "utf8"));
const listJson = (dir) => (existsSync(dir) ? readdirSync(dir).filter((f) => f.endsWith(".json")).map((f) => join(dir, f)) : []);

const ajv = new Ajv2020({ allErrors: true, strict: true, strictRequired: false, allowUnionTypes: true, strictTypes: false });
addFormats(ajv);
for (const name of ["facts", "assessment", "reference", "persona", "case", "parameters"]) {
  ajv.addSchema(readJson(join(contracts, `${name}.schema.json`)));
}
const schema = (ref) => ajv.getSchema(BASE + ref) ?? fail(`brak schematu ${ref}`);

let errors = 0;
let checked = 0;
function fail(msg) {
  errors++;
  console.error(`BŁĄD  ${msg}`);
}
function check(file, ref) {
  checked++;
  const data = readJson(file);
  const validate = schema(ref);
  if (!validate(data)) {
    for (const e of validate.errors) fail(`${rel(file)}: ${e.instancePath || "/"} ${e.message} ${JSON.stringify(e.params)}`);
  }
  return data;
}
const rel = (p) => p.replace(root, "").replaceAll("\\", "/").replace(/^\//, "");

function peselChecksumValid(pesel) {
  const w = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];
  const sum = w.reduce((s, x, i) => s + x * Number(pesel[i]), 0);
  return (10 - (sum % 10)) % 10 === Number(pesel[10]);
}

for (const file of listJson(join(fixtures, "reference"))) {
  const data = check(file, "reference.schema.json");
  if (data.id !== basename(file, ".json")) fail(`${rel(file)}: id '${data.id}' ≠ nazwa pliku`);
}

const personaDirs = [join(fixtures, "personas")];
if (process.env.CASES_PRIVATE_DIR) personaDirs.push(join(process.env.CASES_PRIVATE_DIR, "personas"));
for (const dir of personaDirs) {
  for (const file of listJson(dir)) {
    const data = check(file, "persona.schema.json");
    if (data.id !== basename(file, ".json")) fail(`${rel(file)}: id '${data.id}' ≠ nazwa pliku`);
    if (data.tozsamosc?.pesel && peselChecksumValid(data.tozsamosc.pesel)) {
      fail(`${rel(file)}: PESEL przechodzi sumę kontrolną — musi być fikcyjny (zasada 2.5)`);
    }
  }
}

if (existsSync(join(fixtures, "cases.json"))) {
  const data = check(join(fixtures, "cases.json"), "case.schema.json");
  const ids = new Set();
  for (const c of data.cases ?? []) {
    if (ids.has(c.id)) fail(`fixtures/cases.json: zduplikowane id ${c.id}`);
    ids.add(c.id);
  }
}

const seed = check(join(fixtures, "parameters.seed.json"), "parameters.schema.json");
const seen = new Set();
for (const p of seed.parametry ?? []) {
  const k = `${p.key}@${p.validFrom}`;
  if (seen.has(k)) fail(`parameters.seed.json: zduplikowany wiersz ${k}`);
  seen.add(k);
}

const examples = join(contracts, "examples");
check(join(examples, "assessment.example.json"), "assessment.schema.json");
check(join(examples, "assessment-request.example.json"), "assessment.schema.json#/$defs/AssessmentRequest");

// openapi.yaml: składnia + rozwiązywalność wszystkich $ref
checked++;
const openapi = yaml.load(readFileSync(join(contracts, "openapi.yaml"), "utf8"));
const pointer = (doc, ptr) =>
  ptr.split("/").slice(1).reduce((o, k) => (o == null ? undefined : o[decodeURIComponent(k.replaceAll("~1", "/").replaceAll("~0", "~"))]), doc);
(function walk(node, path) {
  if (Array.isArray(node)) return node.forEach((n, i) => walk(n, `${path}/${i}`));
  if (node && typeof node === "object") {
    if (typeof node.$ref === "string") {
      const [file, frag = ""] = node.$ref.split("#");
      const target = file ? (existsSync(join(contracts, file)) ? readJson(join(contracts, file)) : undefined) : openapi;
      if (target === undefined) fail(`openapi.yaml ${path}: brak pliku ${file}`);
      else if (frag && pointer(target, frag) === undefined) fail(`openapi.yaml ${path}: nierozwiązany $ref ${node.$ref}`);
    }
    for (const [k, v] of Object.entries(node)) walk(v, `${path}/${k}`);
  }
})(openapi, "");
const expectedPaths = [
  "/health", "/api/sim/login", "/api/sim/personas", "/api/sim/me/facts", "/api/sim/me/zus", "/api/sim/me/debt-certificate",
  "/api/assessment", "/api/assessment/compare", "/api/submissions", "/api/cases", "/api/cases/by-hash/{sha256}",
  "/api/organizations", "/api/admin/parameters", "/api/admin/parameters/history", "/api/admin/catalog",
  "/api/admin/integrations", "/api/admin/organizations", "/api/assistant/ask",
];
for (const p of expectedPaths) if (!openapi.paths?.[p]) fail(`openapi.yaml: brak ścieżki ${p}`);

console.log(`Sprawdzono plików: ${checked}. Błędów: ${errors}.`);
process.exit(errors ? 1 : 0);
