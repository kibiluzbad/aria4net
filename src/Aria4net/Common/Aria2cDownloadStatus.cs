using System.Collections.Generic;

namespace Aria4net.Common
{
    public class Aria2cDownloadStatus
    {
        public string Gid { get; set; }
        public string Status { get; set; }
        public double TotalLength { get; set; }
        public double CompletedLength { get; set; }
        public double UploadLength { get; set; }
        public string Bitfield { get; set; }
        public double DownloadSpeed { get; set; }
        public double UploadSpeed { get; set; }
        public string InfoHash { get; set; }
        public int? NumSeeders { get; set; }
        public string PieceLength { get; set; }
        public int? NumPieces { get; set; }
        public int? Connections { get; set; }
        public string ErrorCode { get; set; }
        public string[] FollowedBy { get; set; }
        public string BelongsTo { get; set; }
        public string Dir { get; set; }
        public IEnumerable<Aria2cFile> Files { get; set; }
        // public Aria2cBittorrent Bittorrent { get; set; }

        public bool Completed
        {
            get { return 0 < CompletedLength && CompletedLength == TotalLength; }
        }

        public double Progress
        {
            get { return CompletedLength * 100 / TotalLength; }
            
        }

        public double Remaining
        {
            get { return TotalLength - CompletedLength; }

        }

        public double Eta
        {
            get { return (TotalLength - CompletedLength) / DownloadSpeed; }
            
        }
    }
}