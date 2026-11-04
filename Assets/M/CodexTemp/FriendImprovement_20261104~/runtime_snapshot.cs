var lesson = UnityEngine.Object.FindFirstObjectByType<ProjectWait.M.Elevator.ElevatorLesson>();
var mesh = lesson.friend.GetComponentInChildren<SkinnedMeshRenderer>();
var animator = lesson.friend.GetComponentInChildren<Animator>();
var expressions = lesson.friend.GetComponentInChildren<ProjectWait.M.FriendImprovedExpressions>();
return new {
    frame = Time.frameCount,
    state = lesson.Snapshot(),
    model = expressions.name,
    animatorInitialized = animator.isInitialized,
    boneCount = mesh.bones.Length,
    badBones = mesh.bones.Count(b => !b),
    materialCount = mesh.sharedMaterials.Length,
    submeshCount = mesh.sharedMesh.subMeshCount,
    shapes = Enumerable.Range(0, mesh.sharedMesh.blendShapeCount).Select(i => new { name=mesh.sharedMesh.GetBlendShapeName(i), weight=mesh.GetBlendShapeWeight(i) }).ToArray(),
    feet = mesh.bones.Where(b => b.name.StartsWith("foot.")).Select(b => new { b.name, position=new[]{b.position.x,b.position.y,b.position.z} }).ToArray(),
    phone = lesson.friend.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Cabin_Optional119Phone"),
    highlight = lesson.friend.GetComponent<ProjectWait.M.Elevator.ElevatorOutlineTarget>().geometry.All(g=>g.IsValid)
};
