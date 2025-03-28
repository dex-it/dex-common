﻿namespace Dex.Cap.Common.Interfaces
{
    public interface IOutboxMessage
    {
        /// <summary>
        /// Id сообщения аутбокса
        /// </summary>
        /// <remarks>
        /// Свойство не должно быть с приватным сеттером т.к. оно сериализуется <see cref="System.Text.Json"/>.
        /// Если оставить свойство только как get, то наследники должны реализовать его как init, либо как set из-за сериализации. 
        /// Для автопубликации сообщения через MT в интерфейсе свойство должно быть либо set, либо get-only.
        /// Установка свойства как init приведет к ошибке MassTransit <see cref="System.TypeLoadException"/>.
        /// Т.к. MT под капотом пытается создать прокси-объект для интерфейса и падает с ошибкой если ему это не удается сделать.
        /// Но ошибку MT где-то проглатывает внутри и сообщение все-таки публикуется.
        /// </remarks>
        Guid MessageId { get; }
    }
}