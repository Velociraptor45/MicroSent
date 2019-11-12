# This package will contain the spiders of your Scrapy project
#
# Please refer to the documentation for information on how to create and manage
# your spiders.
import scrapy

class EmojiCrawler(scrapy.Spider):
    name = "EmojiCrawler"

    def start_requests(self):
        urls = [
            'http://kt.ijs.si/data/Emoji_sentiment_ranking/',
            ]
        for url in urls:
            yield scrapy.Request(url=url, callback=self.parse)

    def parse(self, response):
        self.appendToFile("Unicode Codepoint,Occurences,Negative,Neural,Positive,Sent score\n")
        table = response.css('#myTable')[0].css('tbody').css('tr')
        for row in table:
            cells = row.css('td')
            cellstring = \
                cells[2].xpath("text()").extract_first() + "," + cells[3].xpath("text()").extract_first() \
                + "," + cells[5].xpath("text()").extract_first() + "," + cells[6].xpath("text()").extract_first() \
                + "," + cells[7].xpath("text()").extract_first() + "," + cells[8].xpath("text()").extract_first() + "\n"
            self.appendToFile(cellstring)

    def appendToFile(self, string):
        with open("emojis.csv", "a") as file:
            print("writing to file: " + string)
            file.write(string)