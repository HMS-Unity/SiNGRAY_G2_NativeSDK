using System.Collections.Generic;

namespace Singray.Foundation {
public class XvAitalkModels
{
   public class cwmodel
    {
        public int gm;
        public int id;//Defined semantic ID
        public int sc;//Confidence
        public string w;//Recognized word
    
    };

    public class wsmodel{
        public int bg;

        public string slot;

        public List<cwmodel>cw;

    }

    public class result{
        public int sn;
        public bool  ls;

        public int bg;

        public int ed;

        public int sc;//Confidence

        public List<wsmodel>ws;
    }

    public class awvResult
    {
        public string sst;
        public int id;

        public int score;

        public int bos;

        public int eos;

        public string keyword;
    }
}
}
