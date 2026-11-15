using System.Reflection;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

internal sealed class SpeechAssistantDialogStateDescriber
{
    internal string Describe(object service)
    {
        ArgumentNullException.ThrowIfNull(service);

        try
        {
            var hostField = service.GetType().GetField("_host", BindingFlags.Instance | BindingFlags.NonPublic);
            if (hostField?.GetValue(service) is not { } host)
            {
                return "host=<null>";
            }

            var stackField = host.GetType().GetField("_stack", BindingFlags.Instance | BindingFlags.NonPublic);
            if (stackField?.GetValue(host) is not System.Collections.IList { Count: > 0 } stack)
            {
                return $"host={host.GetType().Name} stack=<unknown>";
            }

            var activeFlow = stack[stack.Count - 1];
            if (activeFlow is null)
            {
                return $"host={host.GetType().Name} flow=<null>";
            }

            var machineField = activeFlow.GetType().GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic);
            if (machineField?.GetValue(activeFlow) is not { } machine)
            {
                return $"host={host.GetType().Name} flow={activeFlow.GetType().Name} machine=<unknown>";
            }

            var stateProperty = machine.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.Public);
            var stateValue = stateProperty?.GetValue(machine);
            return stateValue is null
                ? $"host={host.GetType().Name} flow={activeFlow.GetType().Name} state=<null>"
                : $"host={host.GetType().Name} flow={activeFlow.GetType().Name} state={stateValue}";
        }
        catch (Exception ex)
        {
            return $"state=<unavailable> ({ex.GetType().Name})";
        }
    }
}

