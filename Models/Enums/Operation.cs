namespace Models.Enums
{
    public class Operation
    {
        public enum OperationType
        {
            WaitKeywordForSearch, WaitActorInfoForSearch, WaitYearsForSearch, WaitAccuracyYearToSearch
        }

        public enum CallbackType
        {
            SearchByActor
        }
    }
}