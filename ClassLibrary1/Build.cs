using System;

namespace ClassLibrary1
{
    public class Build
    {
        public string Id { get; set; }
        //public string TypeId { get; set; }
        public string Name { get; set; }
        public string ProjectName { get; set; }
        //public string State { get; set; }
        public string Status { get; set; } //enum SUCCESS, ERROR, RUNNING
        public string PreviousStatus { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Number { get; set; }
        //public int Sequence { get; set; }

        //public Build()
        //{
        //    Id = Guid.NewGuid().ToString();
        //}

        //public Build(int sequence): this()
        //{
        //    Sequence = sequence;
        //}
    }
}
