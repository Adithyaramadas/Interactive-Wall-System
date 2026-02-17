🎯 Interactive Wall System

AI-Based Pseudo-Touch Detection Using MediaPipe 3D Hand Tracking and Unity**

📌 Project Overview

The Interactive Wall System is an AI-powered touchless interaction platform that simulates wall-touch detection using computer vision and depth-based finger proximity analysis.

The system captures real-time hand movements through a webcam, extracts 3D landmark data using MediaPipe, and determines pseudo-touch events based on Z-axis proximity, stability, and duration thresholds. The detected touch events are transmitted to Unity via UDP communication for interactive media control.

This project demonstrates the integration of **Computer Vision, Real-Time Signal Processing, and Game Engine Interaction**.

🧠 Technologies Used

 🔹 Python Module

* MediaPipe (3D Hand Tracking)
* OpenCV (Video Capture & Frame Processing)
* NumPy (Numerical Computation)
* UDP Socket Programming

🔹 Unity Module

* C#
* Unity VideoPlayer
* UDP Receiver Script (`FingerDataReceiver.cs`)


⚙️ System Architecture

Webcam
   ↓
Python (MediaPipe 3D Hand Tracking)
   ↓
Pseudo-Touch Detection Algorithm
   ↓
UDP Communication (Port 5053)
   ↓
Unity Application
   ↓
Interactive Media Control


🔍 Core Algorithm – Pseudo-Touch Detection

The touch detection logic is based on:

1. 3D Hand Landmark Detection
2. Z-axis depth extraction (Index Finger Tip)
3. Wall calibration
4. Proximity threshold check
5. Stability check
6. Duration threshold validation
7. Touch signal generation (1 / 0)
8. UDP transmission

This ensures:

* Reduced false positives
* Stable interaction
* Real-time responsiveness

📂 Project Structure


Interactive-Wall-System/
│
├── Unity/
│   └── FingerDataReceiver.cs
│
├── Python/
│   └── pseudo_touch_detection.py
│
├── requirements.txt
└── README.md


 🚀 How to Run

### Step 1 – Install Dependencies

```bash
pip install -r requirements.txt
```

### Step 2 – Run Python Module

```bash
python pseudo_touch_detection.py
```
 Step 3 – Open Unity Project

* Attach `FingerDataReceiver.cs`
* Ensure UDP port is set to **5053**
* Run the scene

 🎯 Key Features

✅ Real-time 3D hand tracking
✅ Depth-based finger proximity detection
✅ Calibration-based wall mapping
✅ Stability & duration validation
✅ UDP-based communication
✅ Interactive Unity media control

 📚 Academic Relevance

This project demonstrates practical implementation of:

* Human-Computer Interaction (HCI)
* Computer Vision
* Depth-Based Gesture Recognition
* Real-Time Systems
* Cross-Platform Communication (Python ↔ Unity)

 📌 Applications

* Smart Classrooms
* Interactive Museums
* Gesture-Controlled Presentations
* Touchless Interfaces
* Digital Installations

