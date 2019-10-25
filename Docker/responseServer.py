import socket
import sys

class ResponseServer:
    HOST = '0.0.0.0'
    PORT = 6050

    def openServer(self, message):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as soc:
            soc.bind((self.HOST, self.PORT))
            soc.listen(1)
            con, addr = soc.accept()
            with con:
                print("Connection established with ", addr)
                byteMessage = bytes(message, 'utf-8')
                print("sending message: {}".format(message))
                con.sendall(byteMessage)


if __name__ == '__main__':
    server = ResponseServer()
    message = sys.argv[1]
    server.openServer(message)