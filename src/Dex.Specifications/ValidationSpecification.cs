namespace Dex.Specifications
{
    /// <summary>
    /// Спецификация для проверки корректности объекта заданного типа
    /// </summary>
    /// <typeparam name="T">Тип объекта, для которого применяется спецификация</typeparam>
    public abstract class ValidationSpecification<T> : Specification<T>
    {
        /// <summary>
        /// Удовлетворяет ли объект спецификации
        /// </summary>
        /// <param name="item">Проверяемый объект</param>
        /// <returns>Сообщение об ошибке</returns>
        public string Validate(T item)
        {
            return IsSatisfiedBy(item) ? string.Empty : BuildErrorMessage(item);
        }

        /// <summary>
        /// Построить сообщение об ошибке
        /// </summary>
        /// <param name="item">Проверяемый объект</param>
        /// <returns>Сообщение об ошибке</returns>
        protected abstract string BuildErrorMessage(T item);
    }
}