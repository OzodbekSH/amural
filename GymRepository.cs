using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Gym
{
    public class GymRepository : IDisposable
    {
        private readonly GymDbContext _db = new GymDbContext();

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

        // ── Свойства ─────────────────────────────────────────────────────────

        public IReadOnlyList<Member>                Members       { get { return _db.Members.OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToList(); } }
        public IReadOnlyList<Subscription>          Subscriptions { get { return _db.Subscriptions.OrderByDescending(s => s.IsActive).ThenBy(s => s.EndDate).ToList(); } }
        public IReadOnlyList<Visit>                 Visits        { get { return _db.Visits.OrderByDescending(v => v.VisitDate).ToList(); } }
        public IReadOnlyList<UserAccount>           Users         { get { return _db.Users.OrderBy(u => u.RoleString).ThenBy(u => u.DisplayName).ToList(); } }
        public IReadOnlyList<string>                PlanCatalog   { get { return _planCatalog; } }
        public IReadOnlyDictionary<string, decimal> PlanPrices    { get { return _planPrices; } }

        // ── Аутентификация / регистрация ─────────────────────────────────────

        public UserAccount Authenticate(string login, string password)
        {
            return _db.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
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
            if (_db.Users.Any(u => u.Login == login.Trim()))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var member = AddMemberWithSubscription(
                        firstName, lastName, birthDate, phone, email, notes,
                        planName, DateTime.Today, durationMonths, price);

                    var user = AddUser(login, password, UserRole.Client,
                        firstName.Trim() + " " + lastName.Trim(), member.MemberId);

                    tx.Commit();
                    return user;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
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
                FirstName = firstName.Trim(),
                LastName  = lastName.Trim(),
                BirthDate = birthDate.Date,
                Phone     = phone.Trim(),
                Email     = (email  ?? string.Empty).Trim(),
                Notes     = (notes  ?? string.Empty).Trim()
            };
            _db.Members.Add(member);
            _db.SaveChanges();
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
            _db.SaveChanges();
        }

        public void DeleteMember(int memberId)
        {
            var member = GetMember(memberId);
            _db.Visits.RemoveRange(_db.Visits.Where(v => v.MemberId == memberId));
            _db.Subscriptions.RemoveRange(_db.Subscriptions.Where(s => s.MemberId == memberId));
            _db.Users.RemoveRange(_db.Users.Where(u => u.MemberId == memberId && u.RoleString == UserRole.Client.ToString()));
            _db.Members.Remove(member);
            _db.SaveChanges();
        }

        public List<Member> GetVisibleMembers(UserAccount user)
        {
            if (user?.Role == UserRole.Client && user.MemberId.HasValue)
                return _db.Members.Where(m => m.MemberId == user.MemberId.Value).ToList();
            return _db.Members.OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToList();
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
                SubscriptionNumber = GetNextSubscriptionNumber(),
                MemberId  = memberId,
                PlanName  = planName.Trim(),
                StartDate = startDate.Date,
                EndDate   = endDate.Date,
                Price     = price,
                IsActive  = isActive
            };
            _db.Subscriptions.Add(sub);
            _db.SaveChanges();
            return sub;
        }

        public void UpdateSubscription(int subscriptionId, int memberId, string planName,
            DateTime startDate, DateTime endDate, decimal price, bool isActive)
        {
            GetMember(memberId);
            ValidatePlanName(planName);
            var sub = _db.Subscriptions.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
            if (sub == null)
                throw new InvalidOperationException("Абонемент не найден.");
            if (endDate.Date <= startDate.Date)
                throw new InvalidOperationException("Дата окончания должна быть позже даты начала.");
            if (price < 0)
                throw new InvalidOperationException("Стоимость не может быть отрицательной.");
            if (isActive && _db.Subscriptions.Any(s =>
                s.SubscriptionId != subscriptionId && s.MemberId == memberId &&
                s.IsActive && s.StartDate <= startDate.Date && s.EndDate >= startDate.Date))
                throw new InvalidOperationException("У клиента уже есть активный абонемент.");

            sub.MemberId  = memberId;
            sub.PlanName  = planName.Trim();
            sub.StartDate = startDate.Date;
            sub.EndDate   = endDate.Date;
            sub.Price     = price;
            sub.IsActive  = isActive;
            _db.SaveChanges();
        }

        public void DeleteSubscription(int subscriptionId)
        {
            var sub = _db.Subscriptions.Find(subscriptionId);
            if (sub != null)
            {
                _db.Subscriptions.Remove(sub);
                _db.SaveChanges();
            }
        }

        public List<Subscription> GetVisibleSubscriptions(UserAccount user)
        {
            IQueryable<Subscription> query = _db.Subscriptions;
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
            if (_db.Visits.Any(v => v.MemberId == memberId && v.VisitDate == visitDate.Date))
                throw new InvalidOperationException("Посещение уже зарегистрировано на эту дату.");

            // Use raw SQL to bypass EF's OUTPUT INSERTED issue with the INSTEAD OF INSERT trigger
            _db.Database.ExecuteSqlCommand(
                "INSERT INTO Visits (MemberId, VisitDate, RegisteredByUserId, Comment) VALUES (@p0, @p1, @p2, @p3)",
                memberId, visitDate.Date, registeredByUserId, (object)(comment?.Trim() ?? "") ?? DBNull.Value);

            return _db.Visits
                .Where(v => v.MemberId == memberId && v.VisitDate == visitDate.Date)
                .OrderByDescending(v => v.VisitId)
                .First();
        }

        public void UpdateVisit(int visitId, int memberId, DateTime visitDate, string comment)
        {
            GetMember(memberId);
            var visit = _db.Visits.Find(visitId);
            if (visit == null)
                throw new InvalidOperationException("Посещение не найдено.");
            if (!HasActiveSubscription(memberId, visitDate))
                throw new InvalidOperationException("Нельзя зарегистрировать посещение без активного абонемента.");
            if (_db.Visits.Any(v => v.VisitId != visitId && v.MemberId == memberId && v.VisitDate == visitDate.Date))
                throw new InvalidOperationException("Посещение уже зарегистрировано на эту дату.");

            visit.MemberId  = memberId;
            visit.VisitDate = visitDate.Date;
            visit.Comment   = (comment ?? string.Empty).Trim();
            _db.SaveChanges();
        }

        public void DeleteVisit(int visitId)
        {
            var visit = _db.Visits.Find(visitId);
            if (visit != null)
            {
                _db.Visits.Remove(visit);
                _db.SaveChanges();
            }
        }

        public List<Visit> GetVisibleVisits(UserAccount user)
        {
            IQueryable<Visit> query = _db.Visits;
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
            if (_db.Users.Any(u => u.Login == login.Trim()))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");
            if (role == UserRole.Client)
            {
                if (!memberId.HasValue)
                    throw new InvalidOperationException("Для клиента необходимо выбрать карточку клиента.");
                GetMember(memberId.Value);
                if (_db.Users.Any(u => u.RoleString == UserRole.Client.ToString() && u.MemberId == memberId.Value))
                    throw new InvalidOperationException("Для этого клиента уже создан пользователь.");
            }

            var user = new UserAccount
            {
                Login       = login.Trim(),
                Password    = password,
                Role        = role,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? login.Trim() : displayName.Trim(),
                MemberId    = memberId
            };
            _db.Users.Add(user);
            _db.SaveChanges();
            return user;
        }

        public void UpdateUser(int userId, string login, string password, UserRole role,
            string displayName, int? memberId)
        {
            var user = _db.Users.Find(userId);
            if (user == null)
                throw new InvalidOperationException("Пользователь не найден.");
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Логин и пароль обязательны.");
            if (_db.Users.Any(u => u.UserId != userId && u.Login == login.Trim()))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");
            if (role == UserRole.Client)
            {
                if (!memberId.HasValue)
                    throw new InvalidOperationException("Для клиента необходимо выбрать карточку клиента.");
                GetMember(memberId.Value);
                if (_db.Users.Any(u => u.UserId != userId && u.RoleString == UserRole.Client.ToString() && u.MemberId == memberId.Value))
                    throw new InvalidOperationException("Для этого клиента уже создан пользователь.");
            }

            user.Login       = login.Trim();
            user.Password    = password;
            user.Role        = role;
            user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? login.Trim() : displayName.Trim();
            user.MemberId    = role == UserRole.Client ? memberId : null;
            _db.SaveChanges();
        }

        public void DeleteUser(int userId)
        {
            var user = _db.Users.Find(userId);
            if (user != null)
            {
                _db.Users.Remove(user);
                _db.SaveChanges();
            }
        }

        public List<UserAccount> GetVisibleUsers(UserAccount current)
        {
            if (current?.Role == UserRole.Client)
                return _db.Users.Where(u => u.UserId == current.UserId).ToList();
            if (current?.Role == UserRole.Trainer)
                return _db.Users.Where(u => u.RoleString != UserRole.Administrator.ToString()).ToList();
            return _db.Users.OrderBy(u => u.RoleString).ThenBy(u => u.DisplayName).ToList();
        }

        // ── Отчёты ───────────────────────────────────────────────────────────

        public List<WeeklyVisitReportItem> GetActiveVisitsForWeek()
        {
            return _db.Database.SqlQuery<WeeklyVisitReportItem>(
                "SELECT VisitDate, MemberName, PlanName AS SubscriptionPlan " +
                "FROM ActiveVisitsLastWeek " +
                "ORDER BY VisitDate DESC"
            ).ToList();
        }

        public List<TopVisitorReportItem> GetTopVisitors()
        {
            return _db.Database.SqlQuery<TopVisitorReportItem>(
                "SELECT m.LastName + N' ' + m.FirstName AS MemberName, COUNT(v.VisitId) AS VisitsCount " +
                "FROM Visits v " +
                "JOIN Members m ON m.MemberId = v.MemberId " +
                "GROUP BY m.MemberId, m.LastName, m.FirstName " +
                "ORDER BY VisitsCount DESC, MemberName"
            ).ToList();
        }

        // ── Вспомогательные ──────────────────────────────────────────────────

        public string GetMemberName(int memberId) { return GetMember(memberId).FullName; }

        public bool HasActiveSubscription(int memberId, DateTime onDate)
        {
            var date = onDate.Date;
            return _db.Subscriptions.Any(s =>
                s.MemberId == memberId && s.IsActive &&
                s.StartDate <= date && s.EndDate >= date);
        }

        private string GetPlanForDate(int memberId, DateTime onDate)
        {
            var date = onDate.Date;
            var sub = _db.Subscriptions
                .Where(s => s.MemberId == memberId && s.StartDate <= date && s.EndDate >= date)
                .OrderByDescending(s => s.IsActive).ThenByDescending(s => s.EndDate)
                .FirstOrDefault();
            return sub == null ? "Нет абонемента" : sub.PlanName;
        }

        private Member GetMember(int memberId)
        {
            var m = _db.Members.Find(memberId);
            if (m == null) throw new InvalidOperationException("Клиент не найден.");
            return m;
        }

        private int GetNextSubscriptionNumber()
        {
            var used = new HashSet<int>(_db.Subscriptions.Select(s => s.SubscriptionNumber));
            for (var n = 100; n <= 9999; n++)
                if (!used.Contains(n)) return n;
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

        public void Dispose() { _db?.Dispose(); }
    }
}
