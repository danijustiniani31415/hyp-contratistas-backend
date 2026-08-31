using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Abril_Backend.Application.DTOs
{
    public class IvtControlPdfCreateDTO
    {
        public List<IFormFile> Pdfs {get; set;}
        public int ProjectId {get; set;}
        public DateOnly PeriodDate {get;set;}
    }
}