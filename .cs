using System;
using SLASystem;

namespace TwitchBot
{
    public class SaveSystem
    {
        public SaveSystem()
        {
            User GetUser(string username)
            {
                SLASystem slasystem = new SLASystem();
                slasystem.Load();
            }
        }
    }
}
public struct User{
    public string name;
    public int beans;
}
