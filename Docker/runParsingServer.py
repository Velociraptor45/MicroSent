import socket
import os
import _thread

#src: https://docs.python.org/2/library/simplehttpserver.html
#src: https://realpython.com/python-sockets/#tcp-sockets

class RunParsingServer:
    HOST = '0.0.0.0'
    PORT = 6048
    incommingByteSize = 512

    def openServer(self):
        print("starting to listen on {}:{}".format(self.HOST, self.PORT))
        receivedSentence = None
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as soc:
            soc.bind((self.HOST, self.PORT))
            soc.listen(1)
            while True:
                con, addr = soc.accept()
                with con:
                    print("Connection established with ", addr)
                    data = con.recv(self.incommingByteSize)
                    if data:
                        print("RECEIVED DATA: {}".format(data))
                        receivedSentence = "".join(map(chr, data))
                        _thread.start_new_thread (self.processData, (receivedSentence, None))
                        con.close()
                print("-------------------------------------------------------------")
        print("connection closed")


    def processData(self, sentence, extra):
        os.system("echo {} | syntaxnet/demo.sh".format(sentence))
    

if __name__ == '__main__':
    server = RunParsingServer()
    server.openServer()