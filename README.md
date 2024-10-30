To test Locally from appsettings.Development.json you Run 2 separate api locally with api1 failing every 30 seconds with this python script.

```Python
from flask import Flask, jsonify
from threading import Thread
import time
import os
import subprocess

# Track API 1's subprocess for toggling its state
api1_process = None

# Function to create a Flask app file for a specific API
def create_app_file(api_name, message):
    app_code = f'''
from flask import Flask, jsonify
app = Flask(__name__)

@app.route('/api/source')
def api_source():
    return jsonify(message="{message}")

@app.route('/health')
def health():
    return "Healthy", 200  # HTTP 200 OK indicates healthy status

if __name__ == '__main__':
    app.run()
    '''
    with open(f"{api_name}_app.py", "w") as f:
        f.write(app_code)

# Function to start an app as a separate subprocess
def start_app_in_subprocess(app_name, port):
    global api1_process
    if app_name == "api1":
        # Track API 1's process
        api1_process = subprocess.Popen(["python3", "-m", "flask", "run", "--port", str(port)],
                                        env={**os.environ, "FLASK_APP": f"{app_name}_app.py"})
    else:
        # Start API 2 without tracking (it will stay online)
        subprocess.Popen(["python3", "-m", "flask", "run", "--port", str(port)],
                         env={**os.environ, "FLASK_APP": f"{app_name}_app.py"})

# Function to toggle API 1 availability every 15 seconds
def toggle_api1():
    global api1_process
    while True:
        # Stop API 1
        if api1_process:
            print("Stopping API 1...")
            api1_process.terminate()
            api1_process.wait()
            print("API 1 is now OFFLINE")

        # Wait 15 seconds before restarting API 1
        time.sleep(30)

        # Restart API 1
        print("Starting API 1...")
        start_app_in_subprocess("api1", 5001)
        print("API 1 is now ONLINE")

        # Keep API 1 online for 15 seconds
        time.sleep(30)

if __name__ == '__main__':
    # Create app files for API 1 and API 2
    create_app_file("api1", "API1 is responding")
    create_app_file("api2", "API2 is responding")

    # Start API 2 (remains stable on port 5002)
    Thread(target=start_app_in_subprocess, args=("api2", 5002), daemon=True).start()
    print("API 2 is ONLINE and running on port 5002")

    # Start API 1 initially on port 5001 and then toggle its state
    start_app_in_subprocess("api1", 5001)
    print("API 1 is ONLINE and running on port 5001")

    # Start the toggle function to bring API 1 up and down every 15 seconds
    toggle_thread = Thread(target=toggle_api1, daemon=True)
    toggle_thread.start()

    # Keep the main script running to maintain both threads
    try:
        toggle_thread.join()
    except KeyboardInterrupt:
        print("\nShutting down both APIs gracefully...")
        if api1_process:
            api1_process.terminate()
