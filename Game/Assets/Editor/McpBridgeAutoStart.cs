// 에디터가 켜질 때(스크립트 다시 불러올 때 포함) MCP for Unity 브리지가 꺼져 있으면 켠다.
// Claude가 하이라키에 직접 오브젝트를 만들 수 있게 하기 위한 장치.
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    [InitializeOnLoad]
    static class McpBridgeAutoStart
    {
        static McpBridgeAutoStart()
        {
            EditorApplication.delayCall += TryStart;
        }

        static void TryStart()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost"))
                .FirstOrDefault(t => t != null);
            if (type == null)
            {
                Debug.LogWarning("[CatTang] MCP 브리지 타입을 찾지 못함");
                return;
            }
            var running = (bool)type.GetProperty("IsRunning").GetValue(null);
            if (running) return;
            type.GetMethod("Start", Type.EmptyTypes).Invoke(null, null);
            Debug.Log("[CatTang] MCP 브리지 시작");
        }
    }
}
