import socket


#src: https://docs.python.org/2/library/simplehttpserver.html
#src: https://realpython.com/python-sockets/#tcp-sockets

HOST = '0.0.0.0'
PORT = 6048

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as socket:
    socket.bind((HOST, PORT))
    socket.listen(1)
    conn, addr = socket.accept()
    with conn:
        print("Connection established with ", addr)
        while True:
            data = conn.recv(512)
            print("received data:")
            print(data)
            if not data:
                print("exiting")
                break
            print("sending data")
            conn.sendall(data)
            print("sent data")

    print("end script")