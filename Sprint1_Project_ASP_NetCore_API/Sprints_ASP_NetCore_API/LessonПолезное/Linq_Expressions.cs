using System.Linq.Expressions;


namespace SprintASP_NetCore_API.LessonПолезное
{

    public class Linq_Expressions
    {

        public class User
        {
            public int Age { get; set; }
        }


        public void CreateExpression()
        {

            // Шаг 1. Определение входного параметра
            ParameterExpression userParam = Expression.Parameter(typeof(User), "u"); // Необходимо создать узел, который будет представлять объект u типа User.

            // Шаг 2. Создание узла доступа к свойству
            MemberExpression ageProperty = Expression.Property(userParam, "Age"); // Узел должен описывать операцию обращения к свойству Age параметра u.

            // Шаг 3. Создание узла константы
            ConstantExpression threshold = Expression.Constant(18); // Значение для сравнения должно быть представлено как узел дерева.

            // Шаг 4. Создание узла сравнения
            BinaryExpression comparison = Expression.GreaterThan(ageProperty, threshold); // Узел должен описывать операцию обращения к свойству Age параметра u.
             
            // Шаг 5. Формирование итоговой лямбды
            Expression<Func<User, bool>> lambda = Expression.Lambda<Func<User, bool>>(comparison, userParam); // Тело выражения (сравнение) связывается с параметром.
             
            // Компиляция дерева в делегат Func<User, bool>
            Func<User, bool> compiledFunc = lambda.Compile();

            // Вызов
            var user = new User { Age = 20 };
            bool isAdult = compiledFunc(user); //Результат: true 

            // Ручное создание деревьев выражений необходимо в следующих сценариях:
            // Динамические фильтры
            // Построение запросов к базе данных на основе параметров, выбранных пользователем в интерфейсе(например, фильтрация по произвольному набору колонок).
            // Универсальные репозитории
            // Создание методов, которые принимают имена полей в виде строк и преобразуют их в типизированные выражения.
            // Маппинг данных
            // Оптимизация копирования свойств между объектами разных типов через генерацию и компиляцию кода «на лету».





        }
    }
}
