# Microsent
This is the code repository for David Krammer's Bachelor's thesis. It splits up in separate projects, explained in the following.

## Data Crawling
This project was used to crawl websites using the python library "Scrapy". The files in there are mostly loose python files that have to be executed via a Scrapy command on the command line.

## Docker
Here are all necessary files for starting up the Google Parser "Syntaxnet" in a Docker Container. Please follow the readme in there for further information

## LexiconExtension
This project was used to calculate via machine learning an extended polarity lexicon to use for the main application.

## Microsent
This is the main project. Here you can find all the code that is necessary to execute the sentiment analysis (except for the Syntaxnet Docker Container).

## Serialization
This project was mainly used to serialize data for the main application for faster loading. Most code in here is quite messy and written for one time use only.