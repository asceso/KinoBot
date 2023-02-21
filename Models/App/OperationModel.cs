using static Models.Enums.Operation;

namespace Models.App
{
    /// <summary>
    /// Операция в боте
    /// </summary>
    public class OperationModel
    {
        /// <summary>
        /// ИД пользователя
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Тип операции
        /// </summary>
        public OperationType OperationType { get; set; }

        /// <summary>
        /// Параметры
        /// </summary>
        public Dictionary<string, object> Params { get; set; }

        public OperationModel(long userId, OperationType operationType, params KeyValuePair<string, object>[] operationParams)
        {
            UserId = userId;
            OperationType = operationType;
            Params = new Dictionary<string, object>();
            if (operationParams is not null)
            {
                foreach (var pair in operationParams)
                {
                    try
                    {
                        Params.Add(pair.Key, pair.Value);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
        }
    }
}