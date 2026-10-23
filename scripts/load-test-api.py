#!/usr/bin/env python3
"""Dependency-free HTTP load smoke test for an OFFLINE local API only."""
import argparse
import concurrent.futures
import json
import statistics
import time
import urllib.error
import urllib.request

def send(base, n):
    payload = json.dumps({"question": "worker queue latency", "topK": 3}).encode()
    request = urllib.request.Request(base + "/ask", data=payload,
                                     headers={"Content-Type": "application/json"}, method="POST")
    start = time.perf_counter()
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            response.read()
            return response.status, (time.perf_counter() - start) * 1000
    except urllib.error.HTTPError as error:
        return error.code, (time.perf_counter() - start) * 1000

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", default="http://127.0.0.1:5080")
    parser.add_argument("--requests", type=int, default=100)
    parser.add_argument("--concurrency", type=int, default=10)
    args = parser.parse_args()
    if args.requests < 1 or args.concurrency < 1 or args.requests > 1000 or args.concurrency > 50:
        parser.error("requests must be 1..1000 and concurrency 1..50")
    if not (args.url.startswith("http://127.0.0.1:") or args.url.startswith("http://localhost:")):
        parser.error("Only local loopback URLs are permitted")
    start = time.perf_counter()
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.concurrency) as pool:
        results = list(pool.map(lambda i: send(args.url, i), range(args.requests)))
    elapsed = time.perf_counter() - start
    times = sorted(ms for _, ms in results)
    failures = sum(status != 200 for status, _ in results)
    p95 = times[max(0, int(len(times) * 0.95) - 1)]
    print(json.dumps({"requests": len(results), "failures": failures,
                      "throughput_rps": round(len(results) / elapsed, 2),
                      "p50_ms": round(statistics.median(times), 2),
                      "p95_ms": round(p95, 2)}, indent=2))
    if failures:
        raise SystemExit(1)

if __name__ == "__main__":
    main()
