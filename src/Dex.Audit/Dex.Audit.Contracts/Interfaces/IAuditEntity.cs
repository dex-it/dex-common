namespace Dex.Audit.Contracts.Interfaces;

/// <summary>
/// Интерфейс для пометки сущностей, которые нужно аудировать.
/// </summary>
/// <remarks>
/// Изменения сущности подвергнутся аудированию только если её успел затрекать dbContext.
/// Если произошел сбой до того, как она попала в трекер DbContext, то сообщение аудита сформировано и отправлено не будет.
/// </remarks>
public interface IAuditEntity
{
}
