namespace Clovent.Core.Interfaces;

using Clovent.Core.Results;

public interface IDoctorService
{
    DoctorResult Diagnose(string rootPath);
}
