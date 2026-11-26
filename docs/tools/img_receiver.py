# 브라우저에서 생성한 그림 받기 (ChatGPT CSP가 fetch를 막아서 form POST → 303으로 되돌아감)
# 실행: python docs/tools/img_receiver.py <저장 폴더> [포트=8765]
# 페이지 쪽: <form method=post enctype=multipart/form-data action=http://127.0.0.1:8765/ target=_self> 에 name(파일 이름) + file(이미지) + back(돌아갈 주소)
import cgi, os, sys
from http.server import BaseHTTPRequestHandler, HTTPServer

OUT = sys.argv[1] if len(sys.argv) > 1 else "."
PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8765
os.makedirs(OUT, exist_ok=True)


class H(BaseHTTPRequestHandler):
    def do_POST(self):
        form = cgi.FieldStorage(fp=self.rfile, headers=self.headers, environ={"REQUEST_METHOD": "POST", "CONTENT_TYPE": self.headers["Content-Type"]})
        name = os.path.basename(form.getfirst("name", "img.png")) or "img.png"
        back = form.getfirst("back", "")
        f = form["file"]
        data = f.file.read() if getattr(f, "file", None) else f.value
        path = os.path.join(OUT, name)
        with open(path, "wb") as w:
            w.write(data)
        print("SAVED", path, len(data), flush=True)
        if back:
            self.send_response(303)
            self.send_header("Location", back)
            self.end_headers()
        else:
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b"ok")

    def log_message(self, *a):
        pass


HTTPServer(("127.0.0.1", PORT), H).serve_forever()
