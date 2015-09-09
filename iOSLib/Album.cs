namespace IOSLib
{
    public class Album
    {
        public int Id;
        public string Name;

        private Album() { }
        public Album(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
