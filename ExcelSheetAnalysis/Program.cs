using System.Collections.Generic;
using System.Linq;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;
using System;

namespace ExcelSheetAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            const string FilePath = @"data\emojis.xlsx";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Program p = new Program();
            p.readFile(FilePath);
            p.printOnConsole();
        }

        private Dictionary<char[], int> positiveEmojis = new Dictionary<char[], int>();
        private Dictionary<char[], int> negativeEmojis = new Dictionary<char[], int>();
        private Dictionary<string, int> positiveSmileys = new Dictionary<string, int>();
        private Dictionary<string, int> negativeSmileys = new Dictionary<string, int>();

        private const int PositiveSmileyColumnNumber = 1;
        private const int NegativeSmileyColumnNumber = 2;
        private const int PositiveEmojiColumnNumber = 3;
        private const int NegativeEmojiColumnNumber = 4;

        private const string SheetName = "antw1";

        private void readFile(string filepath)
        {
            // src: https://stackoverflow.com/questions/16079956/does-npoi-have-support-to-xlsx-format
            XSSFWorkbook workbook;
            using(FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(stream);
            }

            ISheet sheet = workbook.GetSheet(SheetName);

            IRow row;
            int rowNumber = 2;
            while((row = sheet.GetRow(rowNumber)) != null)
            {
                cutEmojisFromCell(row.GetCell(PositiveEmojiColumnNumber), positiveEmojis);
                cutEmojisFromCell(row.GetCell(NegativeEmojiColumnNumber), negativeEmojis);
                cutSmileysFromCell(row.GetCell(PositiveSmileyColumnNumber), positiveSmileys);
                cutSmileysFromCell(row.GetCell(NegativeSmileyColumnNumber), negativeSmileys);
                rowNumber++;
            }
        }

        private void cutEmojisFromCell(ICell cell, Dictionary<char[], int> dictionary)
        {
            string[] lines = cell.StringCellValue.Trim().Split('\n');

            foreach(var line in lines)
            {
                addResultToDict(dictionary, line.ToCharArray());
            }
        }

        private void cutSmileysFromCell(ICell cell, Dictionary<string, int> dictionary)
        {
            string[] lines = cell.StringCellValue.Trim().Split('\n');

            foreach (var line in lines)
            {
                addResultToDict(dictionary, line);
            }
        }

        private void addResultToDict(Dictionary<string, int> dictionary, string result)
        {
            var keys = dictionary.Keys.Where(k => k == result).ToList();

            if (keys.Count == 0)
            {
                dictionary.Add(result, 0);
            }
            else if (keys.Count == 1)
            {
                dictionary[keys.First()]++;
            }
            else
            {
                // shoudn't happen
                return;
            }
        }

        private void addResultToDict(Dictionary<char[], int> dictionary, char[] result)
        {
            if (result.Length != 2)
                return;

            var keys = dictionary.Keys.Where(k => k[0] == result[0] && k[1] == result[1]).ToList();

            if(keys.Count == 0)
            {
                dictionary.Add(result, 0);
            }
            else if(keys.Count == 1)
            {
                dictionary[keys.First()]++;
            }
            else
            {
                // shoudn't happen
                return;
            }
        }

        private void printOnConsole()
        {
            Console.WriteLine("####################### POSITIVE EMOJIS #######################");
            List<KeyValuePair<char[], int>> posEmojisList = positiveEmojis.OrderBy(v => v).ToList();
            foreach (var pair in posEmojisList)
            {
                Console.WriteLine($"{pair.Value}");
            }

            Console.WriteLine("####################### NEGATIVE EMOJIS #######################");
            List<KeyValuePair<char[], int>> negEmojisList = positiveEmojis.OrderBy(v => v).ToList();
            foreach (var pair in negEmojisList)
            {
                Console.WriteLine($"{pair.Value}");
            }

            Console.WriteLine("####################### POSITIVE SMILEYS ######################");
            List<KeyValuePair<string, int>> posSmileyList = positiveSmileys.OrderBy(e => e.Value).ToList();
            foreach (var pair in posSmileyList)
            {
                Console.WriteLine($"{pair.Key}\t{pair.Key}");
            }

            Console.WriteLine("####################### NEGATIVE SMILEYS ######################");
            List<KeyValuePair<string, int>> negSmileyList = negativeSmileys.OrderBy(e => e.Value).ToList();
            foreach (var pair in negSmileyList)
            {
                Console.WriteLine($"{pair.Key}\t{pair.Value}");
            }
        }
    }
}
