using System;
using System.Collections.Generic;
using System.Linq;

namespace Gym
{
    public class GymRepository
    {
        // ── Хранилище ────────────────────────────────────────────────────────

        private readonly List<Member>       _members       = new List<Member>();
        private readonly List<Subscription> _subscriptions = new List<Subscription>();
        private readonly List<Visit>        _visits        = new List<Visit>();
        private readonly List<UserAccount>  _users         = new List<UserAccount>();

        private readonly List<string> _planCatalog = new List<string>
        {
            "Студенческий", "Силовой", "Йога", "Полный доступ", "Бассейн"
        };

        private readonly Dictionary<string, decimal> _planPrices = new Dictionary<string, decimal>
        {
            { "Студенческий",  1500m },
            { "Силовой",       3200m },
            { "Йога",          2900m },
            { "Полный доступ", 4100m },
            { "Бассейн",       2600m }
        };

        private int _memberId = 1, _subscriptionId = 1, _visitId = 1, _userId = 1;

        public GymRepository() { Seed(); }

        // ── Свойства ─────────────────────────────────────────────────────────

        public IReadOnlyList<Member>                Members       { get { return _members; } }
        public IReadOnlyList<Subscription>          Subscriptions { get { return _subscriptions; } }
        public IReadOnlyList<Visit>                 Visits        { get { return _visits; } }
        public IReadOnlyList<UserAccount>           Users         { get { return _users; } }
        public IReadOnlyList<string>                PlanCatalog   { get { return _planCatalog; } }
        public IReadOnlyDictionary<string, decimal> PlanPrices    { get { return _planPrices; } }

        // ── Аутентификация / регистрация ─────────────────────────────────────

        public UserAccount Authenticate(string login, string password)
        {
            return _users.FirstOrDefault(u =>
                string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
        }

        public UserAccount RegisterClientAccount(
            string firstName, string lastName, DateTime birthDate,
            string phone, string email, string notes,
            string login, string password, string planName,
            int durationMonths, decimal price)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new InvalidOperationException("Введите логин.");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Введите пароль.");
            if (password.Trim().Length < 4)
                throw new InvalidOperationException("Пароль должен содержать минимум 4 символа.");
            if (_users.Any(u => string.Equals(u.Login, login.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");

            Member member = null;
            try
            {
                member = AddMemberWithSubscription(
                    firstName, lastName, birthDate, phone, email, notes,
                    planName, DateTime.Today, durationMonths, price);

                return AddUser(login, password, UserRole.Client,
                    firstName.Trim() + " " + lastName.Trim(), member.MemberId);
            }
            catch
            {
                if (member != null) DeleteMember(member.MemberId);
                throw;
            }
        }

        // ── Клиенты ──────────────────────────────────────────────────────────

        public Member AddMemberWithSubscription(
            string firstName, string lastName, DateTime birthDate,
            string phone, string email, string notes,
            string planName, DateTime startDate, int durationMonths, decimal price)
        {
            if (durationMonths <= 0)
                throw new InvalidOperationException("Срок абонемента должен быть больше 0 месяцев.");

            var member = AddMember(firstName, lastName, birthDate, phone, email, notes);
            AddSubscription(member.MemberId, planName, startDate, startDate.AddMonths(durationMonths), price, true);
            return member;
        }

        public Member AddMember(string firstName, string lastName, DateTime birthDate,
            string phone, string email, string notes)
        {
            ValidateMemberFields(firstName, lastName, phone);
            var member = new Member
            {
                MemberId  = _memberId++,
                FirstName = firstName.Trim(),
                LastName  = lastName.Trim(),
                BirthDate = birthDate.Date,
                Phone     = phone.Trim(),
                Email     = (email  ?? string.Empty).Trim(),
                Notes     = (notes  ?? string.Empty).Trim()
            };
            _members.Add(member);
            return member;
        }

        public void UpdateMember(int memberId, string firstName, string lastName,
            DateTime birthDate, string phone, string email, string notes)
        {
            ValidateMemberFields(firstName, lastName, phone);
            var m = GetMember(memberId);
            m.FirstName = firstName.Trim();
            m.LastName  = lastName.Trim();
            m.BirthDate = birthDate.Date;
            m.Phone     = phone.Trim();
            m.Email     = (email  ?? string.Empty).Trim();
            m.Notes     = (notes  ?? string.Empty).Trim();
        }

        public void DeleteMember(int memberId)
        {
            var member = GetMember(memberId);
            _visits.RemoveAll(v => v.MemberId == memberId);
            _subscriptions.RemoveAll(s => s.MemberId == memberId);
            _users.RemoveAll(u => u.MemberId == memberId && u.Role == UserRole.Client);
            _members.Remove(member);
        }

        public List<Member> GetVisibleMembers(UserAccount user)
        {
            if (user?.Role == UserRole.Client && user.MemberId.HasValue)
                return _members.Where(m => m.MemberId == user.MemberId.Value).ToList();
            return _members.OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToList();
        }

        // ── Абонементы ───────────────────────────────────────────────────────

        public Subscription AddSubscription(int memberId, string planName,
            DateTime startDate, DateTime endDate, decimal price, bool isActive)
        {
            GetMember(memberId);
            ValidatePlanName(planName);
            if (endDate.Date <= startDate.Date)
                throw new InvalidOperationException("Дата окончания должна быть позже даты начала.");
            if (price < 0)
                throw new InvalidOperationException("Стоимость не может быть отрицательной.");
            if (isActive && HasActiveSubscription(memberId, startDate))
                throw new InvalidOperationException("У клиента уже есть активный абонемент.");

            var sub = new Subscription
            {
                SubscriptionId     = _subscriptionId++,
                SubscriptionNumber = GetNextSubscriptionNumber(),
                MemberId  = memberId,
                PlanName  = planName.Trim(),
                StartDate = startDate.Date,
                EndDate   = endDate.Date,
                Price     = price,
                IsActive  = isActive
            };
            _subscriptions.Add(sub);
            return sub;
        }

        public void UpdateSubscription(int subscriptionId, int memberId, string planName,
            DateTime startDate, DateTime endDate, decimal price, bool isActive)
        {
            GetMember(memberId);
            ValidatePlanName(planName);
            var sub = _subscriptions.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
            if (sub == null)
                throw new InvalidOperationException("Абонемент не найден.");
            if (endDate.Date <= startDate.Date)
                throw new InvalidOperationException("Дата окончания должна быть позже даты начала.");
            if (price < 0)
                throw new InvalidOperationException("Стоимость не может быть отрицательной.");
            if (isActive && _subscriptions.Any(s =>
                s.SubscriptionId != subscriptionId && s.MemberId == memberId &&
                s.IsActive && s.StartDate.Date <= startDate.Date && s.EndDate.Date >= startDate.Date))
                throw new InvalidOperationException("У клиента уже есть активный абонемент.");

            sub.MemberId  = memberId;
            sub.PlanName  = planName.Trim();
            sub.StartDate = startDate.Date;
            sub.EndDate   = endDate.Date;
            sub.Price     = price;
            sub.IsActive  = isActive;
        }

        public void DeleteSubscription(int subscriptionId)
        {
            var sub = _subscriptions.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
            if (sub != null) _subscriptions.Remove(sub);
        }

        public List<Subscription> GetVisibleSubscriptions(UserAccount user)
        {
            var query = _subscriptions.AsEnumerable();
            if (user?.Role == UserRole.Client && user.MemberId.HasValue)
                query = query.Where(s => s.MemberId == user.MemberId.Value);
            return query.OrderByDescending(s => s.IsActive).ThenBy(s => s.EndDate).ToList();
        }

        // ── Посещения ────────────────────────────────────────────────────────

        public Visit RegisterVisit(int memberId, DateTime visitDate, int registeredByUserId, string comment)
        {
            GetMember(memberId);
            if (!HasActiveSubscription(memberId, visitDate))
                throw new InvalidOperationException("Нельзя зарегистрировать посещение без активного абонемента.");
            if (_visits.Any(v => v.MemberId == memberId && v.VisitDate.Date == visitDate.Date))
                throw new InvalidOperationException("Посещение уже зарегистрировано на эту дату.");

            var visit = new Visit
            {
                VisitId            = _visitId++,
                MemberId           = memberId,
                VisitDate          = visitDate.Date,
                RegisteredByUserId = registeredByUserId,
                Comment            = (comment ?? string.Empty).Trim()
            };
            _visits.Add(visit);
            return visit;
        }

        public void UpdateVisit(int visitId, int memberId, DateTime visitDate, string comment)
        {
            GetMember(memberId);
            var visit = _visits.FirstOrDefault(v => v.VisitId == visitId);
            if (visit == null)
                throw new InvalidOperationException("Посещение не найдено.");
            if (!HasActiveSubscription(memberId, visitDate))
                throw new InvalidOperationException("Нельзя зарегистрировать посещение без активного абонемента.");
            if (_visits.Any(v => v.VisitId != visitId && v.MemberId == memberId && v.VisitDate.Date == visitDate.Date))
                throw new InvalidOperationException("Посещение уже зарегистрировано на эту дату.");

            visit.MemberId  = memberId;
            visit.VisitDate = visitDate.Date;
            visit.Comment   = (comment ?? string.Empty).Trim();
        }

        public void DeleteVisit(int visitId)
        {
            var visit = _visits.FirstOrDefault(v => v.VisitId == visitId);
            if (visit != null) _visits.Remove(visit);
        }

        public List<Visit> GetVisibleVisits(UserAccount user)
        {
            var query = _visits.AsEnumerable();
            if (user?.Role == UserRole.Client && user.MemberId.HasValue)
                query = query.Where(v => v.MemberId == user.MemberId.Value);
            return query.OrderByDescending(v => v.VisitDate).ToList();
        }

        // ── Пользователи ─────────────────────────────────────────────────────

        public UserAccount AddUser(string login, string password, UserRole role,
            string displayName, int? memberId)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Логин и пароль обязательны.");
            if (_users.Any(u => string.Equals(u.Login, login.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");
            if (role == UserRole.Client)
            {
                if (!memberId.HasValue)
                    throw new InvalidOperationException("Для клиента необходимо выбрать карточку клиента.");
                GetMember(memberId.Value);
                if (_users.Any(u => u.Role == UserRole.Client && u.MemberId == memberId.Value))
                    throw new InvalidOperationException("Для этого клиента уже создан пользователь.");
            }

            var user = new UserAccount
            {
                UserId      = _userId++,
                Login       = login.Trim(),
                Password    = password,
                Role        = role,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? login.Trim() : displayName.Trim(),
                MemberId    = memberId
            };
            _users.Add(user);
            return user;
        }

        public void UpdateUser(int userId, string login, string password, UserRole role,
            string displayName, int? memberId)
        {
            var user = _users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
                throw new InvalidOperationException("Пользователь не найден.");
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Логин и пароль обязательны.");
            if (_users.Any(u => u.UserId != userId &&
                string.Equals(u.Login, login.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");
            if (role == UserRole.Client)
            {
                if (!memberId.HasValue)
                    throw new InvalidOperationException("Для клиента необходимо выбрать карточку клиента.");
                GetMember(memberId.Value);
                if (_users.Any(u => u.UserId != userId && u.Role == UserRole.Client && u.MemberId == memberId.Value))
                    throw new InvalidOperationException("Для этого клиента уже создан пользователь.");
            }

            user.Login       = login.Trim();
            user.Password    = password;
            user.Role        = role;
            user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? login.Trim() : displayName.Trim();
            user.MemberId    = role == UserRole.Client ? memberId : null;
        }

        public void DeleteUser(int userId)
        {
            var user = _users.FirstOrDefault(u => u.UserId == userId);
            if (user != null) _users.Remove(user);
        }

        public List<UserAccount> GetVisibleUsers(UserAccount current)
        {
            if (current?.Role == UserRole.Client)
                return _users.Where(u => u.UserId == current.UserId).ToList();
            if (current?.Role == UserRole.Trainer)
                return _users.Where(u => u.Role != UserRole.Administrator).ToList();
            return _users.OrderBy(u => u.Role).ThenBy(u => u.DisplayName).ToList();
        }

        // ── Отчёты ───────────────────────────────────────────────────────────

        public List<WeeklyVisitReportItem> GetActiveVisitsForWeek()
        {
            var start = DateTime.Today.AddDays(-6);
            return _visits
                .Where(v => v.VisitDate.Date >= start && v.VisitDate.Date <= DateTime.Today)
                .Where(v => HasActiveSubscription(v.MemberId, v.VisitDate))
                .OrderByDescending(v => v.VisitDate)
                .Select(v => new WeeklyVisitReportItem
                {
                    VisitDate        = v.VisitDate,
                    MemberName       = GetMemberName(v.MemberId),
                    SubscriptionPlan = GetPlanForDate(v.MemberId, v.VisitDate)
                })
                .ToList();
        }

        public List<TopVisitorReportItem> GetTopVisitors()
        {
            return _visits
                .GroupBy(v => v.MemberId)
                .Select(g => new TopVisitorReportItem
                {
                    MemberName  = GetMemberName(g.Key),
                    VisitsCount = g.Count()
                })
                .OrderByDescending(x => x.VisitsCount)
                .ThenBy(x => x.MemberName)
                .ToList();
        }

        // ── Вспомогательные ──────────────────────────────────────────────────

        public string GetMemberName(int memberId) { return GetMember(memberId).FullName; }

        public bool HasActiveSubscription(int memberId, DateTime onDate)
        {
            return _subscriptions.Any(s =>
                s.MemberId == memberId && s.IsActive &&
                s.StartDate.Date <= onDate.Date && s.EndDate.Date >= onDate.Date);
        }

        private string GetPlanForDate(int memberId, DateTime onDate)
        {
            var sub = _subscriptions
                .Where(s => s.MemberId == memberId &&
                            s.StartDate.Date <= onDate.Date && s.EndDate.Date >= onDate.Date)
                .OrderByDescending(s => s.IsActive).ThenByDescending(s => s.EndDate)
                .FirstOrDefault();
            return sub == null ? "Нет абонемента" : sub.PlanName;
        }

        private Member GetMember(int memberId)
        {
            var m = _members.FirstOrDefault(x => x.MemberId == memberId);
            if (m == null) throw new InvalidOperationException("Клиент не найден.");
            return m;
        }

        private int GetNextSubscriptionNumber()
        {
            for (var n = 100; n <= 9999; n++)
                if (_subscriptions.All(s => s.SubscriptionNumber != n)) return n;
            throw new InvalidOperationException("Свободные номера абонементов закончились.");
        }

        private static void ValidateMemberFields(string firstName, string lastName, string phone)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                throw new InvalidOperationException("Имя и фамилия обязательны.");
            if (string.IsNullOrWhiteSpace(phone))
                throw new InvalidOperationException("Телефон обязателен.");
        }

        private void ValidatePlanName(string planName)
        {
            if (string.IsNullOrWhiteSpace(planName) || !_planCatalog.Contains(planName.Trim()))
                throw new InvalidOperationException("Выберите абонемент из списка.");
        }

        // ── Начальные данные ──────────────────────────────────────────────────

        private void Seed()
        {
            var today = DateTime.Today;

            _members.AddRange(new[]
            {
                new Member { MemberId = _memberId++, FirstName = "Иван",   LastName = "Петров",   BirthDate = new DateTime(1996,  4, 12), Phone = "+7 914 000-10-01", Email = "ivan.petrov@gym.local",    Notes = "Утренние тренировки" },
                new Member { MemberId = _memberId++, FirstName = "Анна",   LastName = "Соколова", BirthDate = new DateTime(1999,  7,  3), Phone = "+7 914 000-10-02", Email = "anna.sokolova@gym.local",  Notes = "Предпочитает йогу"   },
                new Member { MemberId = _memberId++, FirstName = "Максим", LastName = "Орлов",    BirthDate = new DateTime(1991,  1, 25), Phone = "+7 914 000-10-03", Email = "maksim.orlov@gym.local",   Notes = "Силовая зона"        },
                new Member { MemberId = _memberId++, FirstName = "Елена",  LastName = "Морозова", BirthDate = new DateTime(1988,  9, 19), Phone = "+7 914 000-10-04", Email = "elena.morozova@gym.local", Notes = "Бассейн и кардио"    },
                new Member { MemberId = _memberId++, FirstName = "Кирилл", LastName = "Волков",   BirthDate = new DateTime(2000, 12,  7), Phone = "+7 914 000-10-05", Email = "kirill.volkov@gym.local",  Notes = "Ожидает продление"   }
            });

            _subscriptions.AddRange(new[]
            {
                new Subscription { SubscriptionId = _subscriptionId++, SubscriptionNumber = 100, MemberId = 1, PlanName = "Силовой",       StartDate = today.AddDays(-20), EndDate = today.AddDays( 10), Price = 3200m, IsActive = true  },
                new Subscription { SubscriptionId = _subscriptionId++, SubscriptionNumber = 101, MemberId = 2, PlanName = "Йога",          StartDate = today.AddDays(-12), EndDate = today.AddDays( 18), Price = 2900m, IsActive = true  },
                new Subscription { SubscriptionId = _subscriptionId++, SubscriptionNumber = 102, MemberId = 3, PlanName = "Полный доступ", StartDate = today.AddDays( -5), EndDate = today.AddDays( 25), Price = 4100m, IsActive = true  },
                new Subscription { SubscriptionId = _subscriptionId++, SubscriptionNumber = 103, MemberId = 4, PlanName = "Бассейн",       StartDate = today.AddDays(-40), EndDate = today.AddDays( -2), Price = 2600m, IsActive = false }
            });

            _users.AddRange(new[]
            {
                new UserAccount { UserId = _userId++, Login = "admin", Password = "admin123", Role = UserRole.Administrator, DisplayName = "Администратор зала", MemberId = null },
                new UserAccount { UserId = _userId++, Login = "coach", Password = "coach123", Role = UserRole.Trainer,       DisplayName = "Старший тренер",     MemberId = null },
                new UserAccount { UserId = _userId++, Login = "anna",  Password = "anna123",  Role = UserRole.Client,        DisplayName = "Анна Соколова",      MemberId = 2    }
            });

            SeedVisit(1, today.AddDays(-9), "Кардио");        SeedVisit(2, today.AddDays(-9), "Растяжка");
            SeedVisit(3, today.AddDays(-9), "Ноги");           SeedVisit(1, today.AddDays(-8), "Спина");
            SeedVisit(2, today.AddDays(-8), "Йога");           SeedVisit(3, today.AddDays(-8), "Грудь");
            SeedVisit(1, today.AddDays(-7), "Функциональная"); SeedVisit(2, today.AddDays(-7), "Пилатес");
            SeedVisit(4, today.AddDays(-7), "Последний активный день");
            SeedVisit(1, today.AddDays(-6), "Интервальная");   SeedVisit(2, today.AddDays(-6), "Дыхательная практика");
            SeedVisit(3, today.AddDays(-6), "Кроссфит");       SeedVisit(1, today.AddDays(-5), "Бокс");
            SeedVisit(2, today.AddDays(-5), "Гибкость");       SeedVisit(3, today.AddDays(-4), "Силовая");
            SeedVisit(1, today.AddDays(-3), "Кор");            SeedVisit(2, today.AddDays(-3), "Баланс");
            SeedVisit(3, today.AddDays(-2), "Руки");           SeedVisit(1, today.AddDays(-1), "Спринт");
            SeedVisit(2, today,             "Восстановление");
        }

        private void SeedVisit(int memberId, DateTime date, string comment)
        {
            _visits.Add(new Visit
            {
                VisitId            = _visitId++,
                MemberId           = memberId,
                VisitDate          = date.Date,
                RegisteredByUserId = 2,
                Comment            = comment
            });
        }
    }
}
