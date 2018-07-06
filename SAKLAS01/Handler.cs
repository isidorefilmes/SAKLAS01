using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAKLAS01
{
    //Process the imput

   public class Handler
    {
        public Dictionary<string, string> Commands { get; set; }

        private string CmdType;

        public Handler()
        {

        }
        public Handler(Dictionary<string, string> dic)
        {
            this.Commands = dic;
        }
        public void Handle(string input) // what time is
        {
            input = input.ToLower(); // 


            try
            {

                CmdType = Commands[input]; // we're good

            }
            catch(Exception exp)
            {
                CmdType = "none";
            }
        }
        public string Response()
        {
            string response = string.Empty;
            switch (CmdType)
            {
                case "WhatTime":
                    response = DateTime.Now.ToLongTimeString();
                    break;
                case "WhatDate":
                    response = DateTime.Now.ToLongTimeString();
                    break;
                case "Restart":
                    break;
                case "Shutdown":
                    break;
                case "None":
                    response = "I Don't understand.";
                    break;
            }
            return response;
        }

        internal void Handle()
        {
            throw new NotImplementedException();
        }
    }
}
