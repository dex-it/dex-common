using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Источник типов сообщений инбокса, ограниченный одной сборкой.
/// </summary>
/// <remarks>
/// Реальный <see cref="Dex.Cap.Inbox.AppDomainInboxMessageTypeSource"/> сканирует
/// <c>AppDomain.CurrentDomain.GetAssemblies()</c> — все контексты загрузки процесса. Под тест-раннером
/// Rider (NUnit engine) тест-сборка живёт в отдельном <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
/// одновременно с <c>Default</c>: один тип сообщения приходит из двух контекстов двумя разными
/// <see cref="Type"/> с общим <see cref="Type.AssemblyQualifiedName"/>, и построение реестра падает с
/// <c>AmbiguousMessageTypeException</c>. Для прода такой отказ верный (сборка реально загружена дважды),
/// но под раннером это ложное срабатывание.
/// <para>
/// Скан ровно одной сборки берёт типы того же экземпляра, что и <paramref name="assembly"/>, то есть из
/// одного контекста загрузки: дубля не возникает. Список типов при этом не приходится вести руками — новый
/// тип сообщения подхватывается автоматически, как и в реальном дискавери.
/// </para>
/// </remarks>
internal sealed class SingleAssemblyInboxMessageTypeSource(Assembly assembly) : IInboxMessageTypeSource
{
    public IEnumerable<Type> GetMessageTypes()
    {
        return assembly.GetTypes()
            .Where(t => typeof(IInboxMessage).IsAssignableFrom(t)
                        && t is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false });
    }
}