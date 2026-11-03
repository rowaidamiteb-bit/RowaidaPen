// مسجل جمل المراحل: صفحة بالمتصفح تعرض الجملة، تسجلها، وتحفظها هنا بمجلد "المراحل"
import http from "http";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const here = path.dirname(fileURLToPath(import.meta.url));
const outDir = path.join(here, "..", "المراحل");
fs.mkdirSync(outDir, { recursive: true });
const lines = fs.readFileSync(path.join(here, "الجمل.txt"), "utf8").split(/\r?\n/).filter(l => l.trim());
const name = n => String(n).padStart(2, "0") + ".wav";

http.createServer((req, res) => {
  const url = new URL(req.url, "http://localhost");
  if (req.method === "GET" && url.pathname === "/") {
    res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
    return res.end(fs.readFileSync(path.join(here, "index.html")));
  }
  if (req.method === "GET" && url.pathname === "/lines") {
    const done = lines.map((_, i) => fs.existsSync(path.join(outDir, name(i + 1))));
    res.writeHead(200, { "Content-Type": "application/json; charset=utf-8" });
    return res.end(JSON.stringify({ lines, done }));
  }
  const m = url.pathname.match(/^\/audio\/(\d+)$/);
  if (m && req.method === "GET") {
    const f = path.join(outDir, name(+m[1]));
    if (!fs.existsSync(f)) { res.writeHead(404); return res.end(); }
    res.writeHead(200, { "Content-Type": "audio/wav", "Cache-Control": "no-store" });
    return res.end(fs.readFileSync(f));
  }
  if (m && req.method === "POST") {
    const chunks = [];
    req.on("data", c => chunks.push(c));
    req.on("end", () => {
      fs.writeFileSync(path.join(outDir, name(+m[1])), Buffer.concat(chunks));
      console.log("حفظت جملة المرحلة", m[1]);
      res.writeHead(200); res.end("ok");
    });
    return;
  }
  res.writeHead(404); res.end();
}).listen(5299, "127.0.0.1", () => console.log("المسجل شغال: http://localhost:5299"));
