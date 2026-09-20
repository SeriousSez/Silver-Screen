namespace SilverScreen.Domain.Writing
{
    public static class WriterAssignmentRules
    {
        public static bool CanAssign(Employee writer)
        {
            if (writer == null || writer.Role != EmployeeRole.Writer) return false;
            bool noConflict = writer.CurrentIntent == null ||
                              writer.CurrentIntent.Purpose == EmployeeIntentPurpose.None ||
                              writer.CurrentIntent.Purpose == EmployeeIntentPurpose.IdleWander;
            return noConflict && (writer.CurrentState == EmployeeState.Idle ||
                   writer.CurrentState == EmployeeState.Walking &&
                   writer.CurrentIntent?.Purpose == EmployeeIntentPurpose.IdleWander);
        }
    }
}
