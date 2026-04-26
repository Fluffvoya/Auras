import re
import subprocess
import threading
import time
from pathlib import Path


class ASISClient:
    _ANSI_RE = re.compile(r"\x1b\[[0-9;]*m")

    def __init__(self, exe_path: str | Path):
        self.exe_path = Path(exe_path)
        self.proc: subprocess.Popen | None = None
        self._buffer: list[str] = []
        self._lock = threading.Lock()
        self._thread: threading.Thread | None = None
        self._running = False

    def _read_stdout(self) -> None:
        while self._running and self.proc:
            try:
                chunk = self.proc.stdout.read(1024) if self.proc.stdout else ""
                if chunk:
                    with self._lock:
                        self._buffer.append(chunk)
                else:
                    time.sleep(0.05)
            except Exception:
                break

    def _get_buffer(self) -> str:
        with self._lock:
            return "".join(self._buffer)

    def _clear_buffer(self) -> None:
        with self._lock:
            self._buffer = []

    def _wait_stable(self, quiet: float = 0.3, timeout: float = 5.0) -> None:
        start = time.time()
        last = self._get_buffer()
        last_change = time.time()

        while time.time() - start < timeout:
            time.sleep(0.05)
            current = self._get_buffer()
            if current != last:
                last = current
                last_change = time.time()
            elif time.time() - last_change >= quiet:
                return

        raise TimeoutError("ASIS.CLI output did not stabilize")

    def start(self) -> None:
        if not self.exe_path.exists():
            raise FileNotFoundError(f"ASIS executable not found: {self.exe_path}")

        self.proc = subprocess.Popen(
            [str(self.exe_path)],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            bufsize=1,
        )
        self._running = True
        self._thread = threading.Thread(target=self._read_stdout, daemon=True)
        self._thread.start()
        self._wait_stable()
        self._clear_buffer()

    def send(self, command: str, timeout: float = 10.0) -> str:
        if self.proc is None or self.proc.poll() is not None:
            raise RuntimeError("ASIS.CLI process is not running")

        self._clear_buffer()
        self.proc.stdin.write(command + "\n")
        self.proc.stdin.flush()
        self._wait_stable(timeout=timeout)
        return self._strip_prompt(self._get_buffer())

    def _strip_prompt(self, text: str) -> str:
        lines = text.splitlines()
        while lines:
            clean = self._ANSI_RE.sub("", lines[-1]).strip()
            if clean == ">" or re.match(r"^\[.+\] >$", clean):
                lines.pop()
            else:
                break
        return "\n".join(lines).strip()

    def close(self) -> None:
        self._running = False
        if self.proc and self.proc.poll() is None:
            try:
                self.proc.stdin.write("exit\n")
                self.proc.stdin.flush()
                self.proc.wait(timeout=3)
            except Exception:
                pass
            finally:
                self.proc.kill()
                self.proc = None
