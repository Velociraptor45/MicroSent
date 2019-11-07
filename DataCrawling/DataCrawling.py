import scrapy


class Crawler(scrapy.Spider):
    name = "crawler"

    def start_requests(self):
        urls = [
            'https://www.noslang.com/dictionary/a/',
            'https://www.noslang.com/dictionary/b/',
            'https://www.noslang.com/dictionary/c/',
            'https://www.noslang.com/dictionary/d/',
            'https://www.noslang.com/dictionary/e/',
            'https://www.noslang.com/dictionary/f/',
            'https://www.noslang.com/dictionary/g/',
            'https://www.noslang.com/dictionary/h/',
            'https://www.noslang.com/dictionary/i/',
            'https://www.noslang.com/dictionary/j/',
            'https://www.noslang.com/dictionary/k/',
            'https://www.noslang.com/dictionary/l/',
            'https://www.noslang.com/dictionary/m/',
            'https://www.noslang.com/dictionary/n/',
            'https://www.noslang.com/dictionary/o/',
            'https://www.noslang.com/dictionary/p/',
            'https://www.noslang.com/dictionary/q/',
            'https://www.noslang.com/dictionary/r/',
            'https://www.noslang.com/dictionary/s/',
            'https://www.noslang.com/dictionary/t/',
            'https://www.noslang.com/dictionary/u/',
            'https://www.noslang.com/dictionary/v/',
            'https://www.noslang.com/dictionary/w/',
            'https://www.noslang.com/dictionary/x/',
            'https://www.noslang.com/dictionary/y/',
            'https://www.noslang.com/dictionary/z/',
        ]
        for url in urls:
            yield scrapy.Request(url=url, callback=self.parse)



    def parse(self, response):
        DICTIONARY_WORD_SELECTOR = '.dictionary-word'
        for wordset in response.css(DICTIONARY_WORD_SELECTOR):
            slang = wordset.css('abbr').css('span').css('a').css('dt').xpath("text()").extract_first().replace(":", "").rstrip()
            meaning = wordset.css('span').css('dd').xpath("text()").extract_first()
            if slang != "":
                self.appendToFile(slang, meaning)


    def appendToFile(self, slang, meaning):
        with open("data/slang.txt", "a") as file:
            file.write(slang + "---" + meaning + "\n")
