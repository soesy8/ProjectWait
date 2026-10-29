var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var root = s.GetRootGameObjects().First(g => g.name == "Elevator_Safety_Lesson");
return new {
    children = root.GetComponentsInChildren<Transform>(true).Where(t => t == root.transform || t.parent == root.transform || t.parent.parent == root.transform).Select(t => new { t.name, parent = t.parent == null ? null : t.parent.name, active = t.gameObject.activeInHierarchy, components = t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray() }).ToArray(),
    renderers = root.GetComponentsInChildren<Renderer>(true).Select(r => new {r.name, type=r.GetType().Name, enabled=r.enabled, active=r.gameObject.activeInHierarchy, materials=r.sharedMaterials.Select(m=>m==null?null:m.name+":"+m.shader.name).ToArray()}).ToArray()
};
