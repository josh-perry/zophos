import subprocess
import time

def open_client(i):
    name = f"Player_{i}"
    subprocess.Popen(["love", "client/src", f"name={name}"])

if __name__ == "__main__":
    for i in range(0, 2):
        open_client(i)
