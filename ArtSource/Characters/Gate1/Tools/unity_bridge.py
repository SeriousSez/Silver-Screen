"""Small stdio client for the project's already installed official Unity relay.

No Editor mutation is implicit: call specifications are read from an explicit JSON file.
"""
import argparse
import json
import os
from pathlib import Path
import queue
import subprocess
import threading
import time

ROOT = Path(__file__).resolve().parents[4]

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('request', nargs='?')
    parser.add_argument('--output')
    parser.add_argument('--code-file')
    parser.add_argument('--eval')
    parser.add_argument('--timeout', type=int, default=110)
    args = parser.parse_args()
    relay = Path.home() / '.unity/relay/relay_win.exe'
    process = subprocess.Popen([str(relay), '--mcp', '--project-path', str(ROOT), '--log', 'error',
                                '--log-dir', os.environ.get('TEMP', str(ROOT / 'Logs'))],
                               stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                               text=True, encoding='utf-8', creationflags=subprocess.CREATE_NO_WINDOW)
    messages = queue.Queue()
    def read():
        for line in process.stdout:
            try: messages.put(json.loads(line))
            except json.JSONDecodeError: pass
    threading.Thread(target=read, daemon=True).start()
    threading.Thread(target=lambda: [None for _ in process.stderr], daemon=True).start()
    def send(value):
        process.stdin.write(json.dumps(value) + '\n')
        process.stdin.flush()
    def call(identifier, method, params):
        send({'jsonrpc':'2.0', 'id':identifier, 'method':method, 'params':params})
        end = time.monotonic() + args.timeout
        while time.monotonic() < end:
            try: msg = messages.get(timeout=min(1, max(.01, end-time.monotonic())))
            except queue.Empty: continue
            if msg.get('id') == identifier: return msg
        raise TimeoutError(method)
    try:
        init = call(1, 'initialize', {'protocolVersion':'2024-11-05', 'capabilities':{},
                                      'clientInfo':{'name':'SilverScreen Gate1', 'version':'1.0'}})
        if 'error' in init: raise RuntimeError(init)
        send({'jsonrpc':'2.0', 'method':'notifications/initialized'})
        request = json.loads(Path(args.request).read_text(encoding='utf-8-sig')) if args.request else {'method':'tools/list', 'params':{}}
        if args.code_file or args.eval:
            body = Path(args.code_file).read_text(encoding='utf-8-sig') if args.code_file else args.eval
            code = 'using UnityEngine; using UnityEditor; internal class CommandScript : IRunCommand { public void Execute(ExecutionResult result) { ' + body + ' } }'
            request = {'method':'tools/call','params':{'name':'Unity_RunCommand','arguments':{'Code':code,'Title':'SilverScreen Gate 1'}}}
        result = call(2, request['method'], request.get('params', {}))
        rendered = json.dumps(result, indent=2, ensure_ascii=False)
        if args.output: Path(args.output).write_text(rendered, encoding='utf-8')
        print(rendered if not args.output else json.dumps({'output':args.output,'error':result.get('error')}))
    finally:
        process.terminate()
        try: process.wait(timeout=5)
        except subprocess.TimeoutExpired: process.kill()

if __name__ == '__main__': main()
