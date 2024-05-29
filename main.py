import time
import socket
import threading
import matplotlib
import matplotlib.pyplot as plt
import numpy as np

matplotlib.use('TkAgg')

localhost = '127.0.0.1'
port = 6340
client: socket.socket

environment = np.zeros((0, 2))
lastCoordinates = np.zeros((359, 2))

exploring = True
robot_global_transform = np.array([[1., 0., 0.], [0., 1., 0.], [0., 0., 1.]])

waypoints: np.array = np.zeros((0, 2))
waypoint_target_index = 0
last_distance_ortho = 1000000000

occupancy_grid = np.zeros((1000, 1000), dtype=bool)


def data_is_lidar(data):
    extracted_string = data[4:9].decode('ascii')

    if extracted_string in {"BATTR", "VERSI", "SWEEP", "G.P.S"}:
        return False
    else:
        return True


def receive_data(client_socket: socket.socket):
    global coordinates
    while True:
        # try:
        data = client_socket.recv(2048)
        if not data:
            print("Connexion perdue avec le robot")
            break

        if data_is_lidar(data):
            coordinates = lidar_bytes_to_cartesian(data)

    # except Exception as e:
    #     print(f"Erreur dans la réception des données : {e}")
    #     break


def interpret_data():
    global coordinates
    while True:
        add_to_environment(coordinates)


def lidar_bytes_to_cartesian(data: bytes) -> np.array:
    # Distance data starts at 728 byte and ends at 1446
    data = data[732:1450]
    distances = np.frombuffer(data, dtype=np.uint16)

    assert len(distances) == 359, "Il n'y a pas les 359 valeurs de distances lidar :/"

    angles = np.linspace(0.5 * np.pi - (2 * np.pi / 360), 2.5 * np.pi, 359, endpoint=False)
    x = distances * np.cos(angles)
    y = distances * np.sin(angles)
    return np.column_stack((x, y))


def transform_points(points: np.array, transformation_matrix: np.array) -> np.array:
    homogenous_points = np.hstack((points, np.ones((points.shape[0], 1))))
    transformed_points = np.dot(homogenous_points, transformation_matrix.T)
    transformed_points[:, 0] = transformed_points[:, 0] + transformation_matrix[2, 0]
    transformed_points[:, 1] = transformed_points[:, 1] - transformation_matrix[2, 1]
    return transformed_points[:, :2]


def add_to_environment(coord: np.array):
    global environment, robot_global_transform, lastCoordinates, exploring

    if np.any(lastCoordinates != 0):
        movement_transform = compute_local_movement(lastCoordinates, coord)
        robot_global_transform = np.matmul(robot_global_transform, movement_transform)

        if True:
            transformed_point_cloud = transform_points(coord, robot_global_transform.T)
            environment = np.append(environment, transformed_point_cloud, axis=0)

    update_last_coordinates(coord)


def compute_local_movement(old_coord: np.array, new_coord: np.array) -> np.array:
    final_matrix = np.eye(3)
    coord2 = new_coord
    for i in range(30):
        pairs = find_closest_pairs(old_coord, coord2)
        iteration_matrix = compute_pair_displacement(old_coord, coord2, pairs)
        old_coord = transform_points(old_coord, iteration_matrix)
        final_matrix = np.matmul(iteration_matrix, final_matrix)

    return final_matrix


def find_closest_pairs(coord1: np.array, coord2: np.array) -> [int]:
    distances = np.linalg.norm(coord1[:, np.newaxis, :] - coord2[np.newaxis, :, :], axis=2)
    pairs = np.argmin(distances, axis=1)
    return pairs


# ECRIT PAR CHATGPT (je comprend pas la SVD)
def compute_pair_displacement(coord1: np.array, coord2: np.array, pairs: [int]) -> [float, float, float]:
    # Extract corresponding points
    paired_points1 = coord1
    paired_points2 = coord2[pairs]

    # Compute centroids
    centroid1 = np.mean(paired_points1, axis=0)
    centroid2 = np.mean(paired_points2, axis=0)

    # Center the points
    centered_points1 = paired_points1 - centroid1
    centered_points2 = paired_points2 - centroid2

    # Compute covariance matrix
    H = np.dot(centered_points1.T, centered_points2)

    # Perform Singular Value Decomposition
    U, S, Vt = np.linalg.svd(H)
    V = Vt.T

    # Compute rotation matrix
    R = np.dot(V, U.T)

    # Ensure a proper rotation matrix (handle reflection case)
    if np.linalg.det(R) < 0:
        V[:, -1] *= -1
        R = np.dot(V, U.T)

    # Compute translation
    t = centroid2 - np.dot(R, centroid1)

    # Combine into a single 3x3 transformation matrix
    T = np.eye(3)
    T[:2, :2] = R
    T[:2, 2] = t

    return T


def update_last_coordinates(coord: np.array):
    global lastCoordinates
    lastCoordinates = coord


def get_distance_waypoint_ortho():
    global robot_global_transform, waypoints, waypoint_target_index

    robot_pos = transform_points(np.array([[0, 0]]), robot_global_transform)
    robot_pos[0, 1] = -robot_pos[0, 1]

    distance = waypoints[waypoint_target_index] - robot_pos

    # Dans le cas ou on se dirige vers le dernier waypoint, on se contente de la distance au prochain point
    if waypoint_target_index + 1 >= waypoints.shape[0]:
        return np.sqrt(np.sum(distance ** 2))

    waypoint_vector = np.array([waypoints[waypoint_target_index + 1] - waypoints[waypoint_target_index]])
    waypoint_vector_normalized = waypoint_vector / np.sqrt(np.sum(waypoint_vector ** 2))
    waypoint_vector_normalized_rotated = np.array([waypoint_vector_normalized[0, 1], -waypoint_vector_normalized[0, 0]])
    orthogonal_distance = np.abs(np.dot(distance, waypoint_vector_normalized_rotated))
    return orthogonal_distance[0]


def get_angle_to_next_waypoint():
    robot_global_orientation = robot_global_transform.copy()
    robot_global_orientation[:, 2] = 0.

    orientation_vector = transform_points(np.array([[0, -1]]), robot_global_orientation)[0]

    robot_pos = transform_points(np.array([[0, 0]]), robot_global_transform)[0]
    robot_pos[1] = -robot_pos[1]

    robot_to_waypoint = (waypoints[waypoint_target_index] - robot_pos)

    robot_angle = -np.arctan2(orientation_vector[1], orientation_vector[0])
    waypoint_angle = np.arctan2(robot_to_waypoint[1], robot_to_waypoint[0])

    angle_diff = robot_angle - waypoint_angle
    angle_diff = (angle_diff + np.pi) % (2 * np.pi) - np.pi
    return angle_diff


def reset_robot_position():
    global robot_global_transform
    robot_global_transform = np.array([[1., 0., 0.], [0., 1., 0.], [0., 0., 1.]])


def send_data(client_socket: socket.socket):
    while True:
        time.sleep(5)


def send_instruction(instruction: str):
    instruction_bytes = instruction.encode('ascii')
    header_bytes = (len(instruction) + 1).to_bytes(4, byteorder='big')
    carriage_return = 0xa.to_bytes()

    complete_message = header_bytes + instruction_bytes + carriage_return

    client.send(complete_message)


def send_instr_argument(instruction: str, argument: int):
    instruction_bytes = instruction.encode('ascii')
    argument_bytes = argument.to_bytes(2, byteorder='little')
    header_bytes = (len(instruction) + 3).to_bytes(4, byteorder='big')
    carriage_return = 0xa.to_bytes()

    complete_message = header_bytes + instruction_bytes + argument_bytes + carriage_return

    client.send(complete_message)


def go_forward():
    send_instruction("ROVFW")


def turn_left():
    send_instruction("ROVTL")


def turn_right():
    send_instruction("ROVTR")


def stop_rover():
    send_instruction("ROVST")


def set_speed(speed: int):
    send_instr_argument("ROVSP", 128 + speed)


def parcours():
    global waypoints, waypoint_target_index, last_distance_ortho
    speed = 30
    while (waypoints.shape[0] < 3):
        time.sleep(0.1)
    while (waypoint_target_index < waypoints.shape[0]):
        virage_a_droite = get_angle_to_next_waypoint() > 0
        if virage_a_droite:
            turn_right()
            while (get_angle_to_next_waypoint() > 0.2):
                set_speed(speed - ((speed - 5) - int(np.abs(get_angle_to_next_waypoint() * 7))))
                time.sleep(0.01)
        else:
            turn_left()
            while (get_angle_to_next_waypoint() < -0.2):
                set_speed(speed - ((speed - 5) - int(np.abs(get_angle_to_next_waypoint() * 7))))
                time.sleep(0.01)

        stop_rover()
        set_speed(speed)

        time.sleep(0.1)

        go_forward()
        last_distance_ortho = get_distance_waypoint_ortho()
        while get_distance_waypoint_ortho() > 1500 and True:
            if get_distance_waypoint_ortho() > 3000 and np.abs(get_angle_to_next_waypoint()) > 0.4:
                # Recommence a tourner, et on decremente waypoint_target_index
                # car il se fait incrementer a la fin de la boucle. Il garde donc la meme valeur
                waypoint_target_index = waypoint_target_index - 1
                break
            last_distance_ortho = get_distance_waypoint_ortho()
            time.sleep(0.01)

        stop_rover()

        time.sleep(0.1)
        waypoint_target_index = waypoint_target_index + 1


def server_setup():
    global client
    global coordinates
    global target
    global environment
    print("Connexion au robot...")
    connected = False
    while not connected:
        try:
            client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            client.connect((localhost, port))
            connected = True
        except Exception as e:
            print(e)
            time.sleep(1)
            pass

    print("Connecté !")

    receive_thread = threading.Thread(target=receive_data, args=(client,))
    interpret_thread = threading.Thread(target=interpret_data)
    send_thread = threading.Thread(target=send_data, args=(client,))
    parcours_thread = threading.Thread(target=parcours)

    receive_thread.start()
    interpret_thread.start()
    send_thread.start()
    parcours_thread.start()

    while True:
        bx.clear()
        bx.scatter(environment[:, 0], environment[:, 1], s=1)
        robot_pos = transform_points(np.array([[0, 0]]), robot_global_transform)
        bx.scatter(robot_pos[0, 0], -robot_pos[0, 1])
        bx.scatter(waypoints[:, 0], waypoints[:, 1])
        bx.set_aspect('equal', 'box')
        plt.draw()
        plt.pause(0.01)

    receive_thread.join()
    send_thread.join()

    client.close()


def on_click(event):
    global waypoints, exploring
    exploring = False
    if event.inaxes:
        waypoints = np.append(waypoints, np.array([[event.xdata, event.ydata]]), axis=0)


if __name__ == "__main__":
    coordinates = np.zeros((360, 2))

    plt.ion()

    fig = plt.figure()
    bx = fig.add_subplot(111)
    plt.connect('button_press_event', on_click)
    plt.show()

    print("Figure montrée")

    server_setup()
