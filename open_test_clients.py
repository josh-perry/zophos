import subprocess
import time

def open_client():
    subprocess.Popen(["love", "client/src"])

if __name__ == "__main__":
    for i in range(0, 5):
        open_client()
        time.sleep(1)
