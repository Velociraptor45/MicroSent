#!/bin/bash

docker cp conll2tree.py $1:/root/models/syntaxnet/syntaxnet/
docker cp runParsingServer.py $1:/root/models/syntaxnet/
docker cp responseServer.py $1:/root/models/syntaxnet/

docker exec $1 'python3.4' 'runParsingServer.py'