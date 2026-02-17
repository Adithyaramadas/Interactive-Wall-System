import cv2
import mediapipe as mp
import socket
import time

# ================== MEDIAPIPE SETUP ==================
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(
    max_num_hands=1,
    min_detection_confidence=0.6,
    min_tracking_confidence=0.6
)
mp_draw = mp.solutions.drawing_utils

# ================== CAMERA ==================
cap = cv2.VideoCapture(0)

# ================== UDP ==================
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
UNITY_IP = "127.0.0.1"
UNITY_PORT = 5053

# ================== PSEUDO TOUCH CONFIG ==================
WALL_Z = None
Z_NEAR = 0.01          # proximity tolerance
Z_STABLE = 0.002       # movement tolerance
STABLE_FRAMES = 4

prev_z = None
stable_count = 0

print("🟢 Touch the wall once to calibrate")

# ================== MAIN LOOP ==================
while True:
    ret, frame = cap.read()
    if not ret:
        break

    frame = cv2.flip(frame, 1)
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    result = hands.process(rgb)

    touch = 0

    if result.multi_hand_landmarks:
        for hand in result.multi_hand_landmarks:
            tip = hand.landmark[8]  # index finger tip
            nx, ny, nz = tip.x, tip.y, tip.z

            # --------- CALIBRATION ---------
            if WALL_Z is None:
                WALL_Z = nz
                print("✅ Wall calibrated at Z =", WALL_Z)

            # --------- PSEUDO TOUCH LOGIC ---------
            if prev_z is not None:
                dz = abs(nz - prev_z)
                near_wall = abs(nz - WALL_Z) < Z_NEAR
                stable = dz < Z_STABLE

                if near_wall and stable:
                    stable_count += 1
                else:
                    stable_count = 0

                if stable_count >= STABLE_FRAMES:
                    touch = 1

            prev_z = nz

            print(f"nx={nx:.2f} ny={ny:.2f} nz={nz:.4f} touch={touch}")

            sock.sendto(
                f"FINGER {nx:.4f} {ny:.4f} {nz:.4f} {touch}".encode(),
                (UNITY_IP, UNITY_PORT)
            )

            mp_draw.draw_landmarks(frame, hand, mp_hands.HAND_CONNECTIONS)

    cv2.imshow("Hand Tracking", frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# ================== CLEANUP ==================
cap.release()
sock.close()
cv2.destroyAllWindows()
