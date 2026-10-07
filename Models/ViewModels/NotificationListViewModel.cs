using System.ComponentModel.DataAnnotations;
using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the Manager's notification report - the filter/paging state plus the matching page of results.</summary>
    public class NotificationListViewModel
    {
        public NotificationChannel? ChannelFilter { get; set; }

        public NotificationType? TypeFilter { get; set; }

        public NotificationStatus? StatusFilter { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalCount { get; set; }

        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public IReadOnlyList<NotificationListItemViewModel> Items { get; set; } = [];
    }
}
