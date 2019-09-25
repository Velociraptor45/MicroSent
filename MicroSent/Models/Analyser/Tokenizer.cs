using System.Text.RegularExpressions;

namespace MicroSent.Models.Analyser
{
    public class Tokenizer
    {

        public Tokenizer()
        {

        }


        public void splitIntoTokens(ref Tweet tweet)
        {
            // " | link | smiley | emoticons | punctuation | sentence structure ()- | wörter
            Regex regex = new Regex(@"(https:\/\/(www\.)?|www\.)([\d\w]+[\.\/])+[\d\w\?\=]+|((:-?|=)(\)|\(|\||\/|(D\b))|\bD:|: [\)\(])|\\U[a-f0-9]{4,8}|([\?!]+|\.+|,|:)|(@|#[a-z]|\\)?(\w([''-]\w)?)+|(\(|\)|-|""|'')");
            tweet.fullText = tweet.fullText.Replace('\n', ' ');

            MatchCollection matches = regex.Matches(tweet.fullText);

            for (int i = 0; i < matches.Count; i++)
            {
                Token t = new Token(matches[i].Value, i);
                tweet.allTokens.Add(t);
            }
        }
    }
}
