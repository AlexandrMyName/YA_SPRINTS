

namespace SprintASP_NetCore_API.Data.Dtos
{

    /// <summary>
    /// DTO для возврата пагинированного результата
    /// </summary>
    /// <typeparam name="T">Тип данных</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Массив элементов на текущей странице
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Общее количество элементов (без учета пагинации)
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Номер текущей страницы (начиная с 1)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Количество элементов на текущей странице
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Общее количество страниц
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// Есть ли предыдущая страница
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Есть ли следующая страница
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Создает пагинированный результат
        /// </summary>
        public static PaginatedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
        {
            return new PaginatedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}