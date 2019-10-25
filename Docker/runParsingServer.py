import socket
import os

#src: https://docs.python.org/2/library/simplehttpserver.html
#src: https://realpython.com/python-sockets/#tcp-sockets

class RunParsingServer:
    HOST = '0.0.0.0'
    PORT = 6048
    incommingByteSize = 512

    def openServer(self):
        print("starting to listen on {}:{}".format(self.HOST, self.PORT))
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as soc:
            soc.bind((self.HOST, self.PORT))
            soc.listen(1)
            con, addr = soc.accept()
            with con:
                print("Connection established with ", addr)
                self.connection = con
                data = con.recv(self.incommingByteSize)
                if data:
                    print("RECEIVED DATA: {}".format(data))
                    string = "".join(map(chr, data))
                    os.system("echo {} | syntaxnet/demo.sh".format(string))
        print("connection closed")
        print("-------------------------------------------------------------")

    
if __name__ == '__main__':
    server = RunParsingServer()
    while True:
        server.openServer()