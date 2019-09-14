using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Models.Enums
{
    //src: https://stackoverflow.com/questions/1833252/java-stanford-nlp-part-of-speech-labels
    public enum PosLabels
    {
        Default,
        CC, //Coordinating conjunction
        CD, //Cardinal number
        DT, //Determiner
        EX, //Existential there
        FW, //Foreign word
        IN, //Preposition or subordinating conjunction
        JJ, //Adjective
        JJR, //Adjective, comparative
        JJS, //Adjective, superlative
        LS, //List item marker
        MD, //Modal
        NN, //Noun, singular or mass
        NNS, //Noun, plural
        NNP, //Proper noun, singular
        NNPS, //Proper noun, plural
        PDT, //Predeterminer
        POS, //Possessive ending
        PRP, //Personal pronoun
        PRPD, // = PRP$; Possessive pronoun
        RB, //Adverb
        RBR, //Adverb, comparative
        RBS, //Adverb, superlative
        RP, //Particle
        SYM, //Symbol
        TO, //to
        UH, //Interjection
        VB, //Verb, base form
        VBD, //Verb, past tense
        VBG, //Verb, gerund or present participle
        VBN, //Verb, past participle
        VBP, //Verb, non­3rd person singular present
        VBZ, //Verb, 3rd person singular present
        WDT, //Wh­determiner
        WP, //Wh­pronoun
        WPD, // = WP$; Possessive wh­pronoun
        WRB //Wh­adverb
    }
}
