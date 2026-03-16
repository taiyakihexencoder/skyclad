using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Mathematics;

namespace skyclad.editor {
	internal static class DataTableLoaderGenerator {
		internal static void Generate() {
			Assembly assembly = typeof(DataTableColumnAttribute).Assembly;
			MakeScripts("DataTableLoaders", assembly);

			foreach(System.Type type in CreateTypeList(assembly)) {
				MakeSystemScript(type);
			}
		}

		private static List<System.Type> CreateTypeList(Assembly assembly) {
			List<System.Type> typeList = new List<System.Type>();
			foreach (System.Type type in assembly.GetTypes()) {
				if (type.IsValueType && !type.IsEnum 
					&& type.GetCustomAttribute<DataTableColumnAttribute>() != null) {
					typeList.Add(type);
				}
			}
			return typeList;
		}

		private static void MakeScripts(string name, Assembly assembly) {
			SourceCodeGenerator gen = new SourceCodeGenerator();
			List<System.Type> typeList = CreateTypeList(assembly);

			gen.AppendLine($"using System.Collections.Generic;");
			gen.AppendLine($"using System.IO;");
			gen.AppendLine($"using System.Threading.Tasks;");
			gen.AppendLine($"using Unity.Entities;");
			gen.AppendLine($"");

			gen.AppendLine($"namespace skyclad {{");
			using (gen.IndentBlock) {
				MakeDataTablesScript(gen, assembly, typeList);

				gen.AppendLine($"");

				foreach(System.Type type in typeList) {
					MakeScript(gen, type);
					gen.AppendLine($"");
				}
			}
			gen.AppendLine($"}}");

			gen.Generate($"dataTable{Path.DirectorySeparatorChar}{name}.cs");
		}

		private static void MakeDataTablesScript(SourceCodeGenerator gen, Assembly assembly, List<System.Type> typeList) {
			string dbName = assembly.GetName().Name[0..1].ToUpper() + assembly.GetName().Name[1..];

			gen.AppendLine($"public static partial class {dbName}DataTables {{");

			Dictionary<System.Type, DataTableColumnAttribute> dex = new Dictionary<System.Type, DataTableColumnAttribute>();
			List<KeyValuePair<string, string>> variables = new List<KeyValuePair<string, string>>();
			foreach (System.Type type in typeList) {
				DataTableColumnAttribute attr = type.GetCustomAttribute<DataTableColumnAttribute>();
				dex.Add(type, attr);
				variables.Add(new KeyValuePair<string, string>($"{type.Name}Loader", $"{attr.tableName}Load"));
			}

			using (gen.IndentBlock) {
				foreach(KeyValuePair<System.Type, DataTableColumnAttribute> type in dex) {
					gen.AppendLine($"private static {type.Key.Name}Loader {type.Value.tableName}Load;");
					gen.AppendLine($"public static BlobAssetReference<SkycladDataBlob<{type.Key.Name}>> {type.Value.tableName} => {type.Value.tableName}Load.assetReference;");
					gen.AppendLine($"");
				}

				// InitTables
				gen.AppendLine($"static partial void InitTables() {{");
				using (gen.IndentBlock) {
					foreach(KeyValuePair<System.Type, DataTableColumnAttribute> type in dex) {
						gen.AppendLine($"{type.Value.tableName}Load = new {type.Key.Name}Loader();");
					}

					gen.AppendLine($"");
					foreach(KeyValuePair<System.Type, DataTableColumnAttribute> type in dex) {
						if (type.Value.tableType == DataTableType.Persistent) {
							gen.AppendLine($"Task.Run(async () => await Load({type.Value.tableName}Load));");
						}
					}
				}
				gen.AppendLine($"}}");

				gen.AppendLine($"");

				// InitIngameTables
				gen.AppendLine($"static partial void InitIngameTables() {{");
				using (gen.IndentBlock) {
					foreach(KeyValuePair<System.Type, DataTableColumnAttribute> type in dex) {
						if (type.Value.tableType == DataTableType.Ingame) {
							gen.AppendLine($"Task.Run(async () => await Load({type.Value.tableName}Load));");
						}
					}
				}
				gen.AppendLine($"}}");

				gen.AppendLine($"");

				// DisposeIngameTables
				gen.AppendLine($"static partial void DisposeIngameTables() {{");
				using (gen.IndentBlock) {
					foreach(KeyValuePair<System.Type, DataTableColumnAttribute> type in dex) {
						if (type.Value.tableType == DataTableType.Ingame) {
							gen.AppendLine($"{type.Value.tableName}Load.Dispose(_mainContext);");
						}
					}
				}
				gen.AppendLine($"}}");

				gen.AppendLine($"");

				// DisposeAllTables
				gen.AppendLine($"static partial void DisposeAllTables() {{");
				using (gen.IndentBlock) {
					foreach(KeyValuePair<System.Type, DataTableColumnAttribute> type in dex) {
						gen.AppendLine($"{type.Value.tableName}Load.Dispose(_mainContext);");
					}
				}
				gen.AppendLine($"}}");

				gen.AppendLine($"");

				foreach(KeyValuePair<System.Type, DataTableColumnAttribute> type in dex) {
					switch(type.Value.tableType) {
						case DataTableType.Manual: {
							gen.AppendLine($"public async static Task Load{type.Key.Name}Table() => await Load({type.Value.tableName}Load);");
							break;
						}
					}
				}
			}

			gen.AppendLine($"}}");
		}

		private static void MakeScript(SourceCodeGenerator gen, System.Type type) {
			int offset = 0;

			List<string> textList = new List<string>();
			System.Type intType = typeof(int), 
				floatType = typeof(float), 
				boolType = typeof(bool),
				float2Type = typeof(float2),
				float3Type = typeof(float3),
				quaternionType = typeof(quaternion);

			int intSize = sizeof(int),
				floatSize = sizeof(float),
				boolSize = sizeof(bool),
				float2Size = floatSize * 2,
				float3Size = floatSize * 3,
				quaternionSize = floatSize * 4;

			List<FieldInfo> fields = new List<FieldInfo>(type.GetFields(BindingFlags.Instance | BindingFlags.Public));
			fields.Sort((a, b) => (a.GetCustomAttribute<OrderAttribute>()?.value ?? -1).CompareTo(b.GetCustomAttribute<OrderAttribute>()?.value ?? -1));
			foreach(FieldInfo field in fields) {
				if (field.FieldType == intType) {
					textList.Add($"{field.Name} = System.BitConverter.ToInt32(bytes, offset + {offset}),");
					offset += intSize;
				} else if (field.FieldType == floatType) {
					textList.Add($"{field.Name} = System.BitConverter.ToSingle(bytes, offset + {offset}),");
					offset += floatSize;
				} else if (field.FieldType == boolType) {
					textList.Add($"{field.Name} = System.BitConverter.ToBoolean(bytes, offset + {offset}),");
					offset += boolSize;
				} else if (field.FieldType == float2Type) {
					textList.Add($"{field.Name} = new float2(");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset}),");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset+floatSize})");
					textList.Add($"),");
					offset += float2Size;
				} else if (field.FieldType == float3Type) {
					textList.Add($"{field.Name} = new float3(");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset}),");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset+floatSize}),");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset+floatSize+floatSize})");
					textList.Add($"),");
					offset += float3Size;
				} else if(field.FieldType == quaternionType) {
					textList.Add($"{field.Name} = new quaternion(");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset}),");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset+floatSize}),");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset+floatSize+floatSize}),");
					textList.Add($"\tSystem.BitConverter.ToSingle(bytes, offset + {offset+floatSize+floatSize+floatSize})");
					textList.Add($"),");
					offset += quaternionSize;
				}
			}

			int dataSize = offset;

			DataTableColumnAttribute attr = type.GetCustomAttribute<DataTableColumnAttribute>();

			gen.AppendLine($"internal class {type.Name}Loader : BaseTableLoader<{type.Name}> {{");
			using (gen.IndentBlock) {
				gen.AppendLine($"override internal string TableName => \"{attr.tableName}\";");
				gen.AppendLine($"");

				gen.AppendLine($"override internal List<{type.Name}> Load(BinaryReader reader) {{");
				using (gen.IndentBlock) {
					gen.AppendLine($"int rows = reader.ReadInt32();");
					gen.AppendLine($"byte[] bytes = reader.ReadBytes(rows * {dataSize});");

					gen.AppendLine($"");

					gen.AppendLine($"int offset = 0;");
					gen.AppendLine($"List<{type.Name}> list = new List<{type.Name}>();");
					gen.AppendLine($"for (int i = 0; i < rows; ++i) {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"list.Add(");
						using (gen.IndentBlock) {
							gen.AppendLine($"new {type.Name} {{");
							using (gen.IndentBlock) {
								foreach(string line in textList) {
									gen.AppendLine(line);
								}
							}
							gen.AppendLine($"}}");
						}
						gen.AppendLine($");");
						gen.AppendLine($"offset += {dataSize};");
					}
					gen.AppendLine($"}}");
					gen.AppendLine($"return list;");
				}
				gen.AppendLine($"}}");

				gen.AppendLine("");

				gen.AppendLine($"override protected void OnComplete(EntityManager entityManager) {{");
				using (gen.IndentBlock) {
					gen.AppendLine($"SendSignal<{type.Name}TableExists>(entityManager, \"Table({attr.tableName})\");");
				}
				gen.AppendLine($"}}");

				gen.AppendLine("");

				gen.AppendLine($"override protected void OnDispose(EntityManager entityManager) {{");
				using (gen.IndentBlock) {
					gen.AppendLine($"SendSignal<Request{type.Name}TableDisposeComponent>(entityManager, \"Dispose Table({attr.tableName})\");");
				}
				gen.AppendLine($"}}");
			}
			gen.AppendLine($"}}");

			gen.AppendLine($"");

			// テーブル読み込み完了フラグ
			gen.AppendLine($"public partial struct {type.Name}TableExists : IComponentData {{ }}");

			// 読み込みフラグの破棄リクエスト
			gen.AppendLine($"internal struct Request{type.Name}TableDisposeComponent : IComponentData {{ }}");
		}

		private static void MakeSystemScript(System.Type type) {
			SourceCodeGenerator gen = new SourceCodeGenerator();
			gen.AppendLine($"using Unity.Collections;");
			gen.AppendLine($"using Unity.Entities;");

			gen.AppendLine($"");

			gen.AppendLine($"namespace skyclad {{");
			using (gen.IndentBlock) {

				gen.AppendLine($"[UpdateInGroup(typeof(SkycladDataTableSystemGroup))]");
				gen.AppendLine($"public partial struct {type.Name}DisposeSystem : ISystem {{");
				using(gen.IndentBlock) {

					gen.AppendLine($"private EntityQuery requestQuery;");
					gen.AppendLine($"private EntityQuery existsQuery;");

					gen.AppendLine($"");

					gen.AppendLine($"void ISystem.OnCreate(ref SystemState state) {{");
					using(gen.IndentBlock) {

						gen.AppendLine($"requestQuery = new EntityQueryBuilder(Allocator.Temp)");
						using(gen.IndentBlock) {
							gen.AppendLine($".WithAll<Request{type.Name}TableDisposeComponent>()");
							gen.AppendLine($".Build(ref state);");
						}
						gen.AppendLine($"state.RequireForUpdate(requestQuery);");

						gen.AppendLine($"");
						gen.AppendLine($"existsQuery = new EntityQueryBuilder(Allocator.Temp)");
						using(gen.IndentBlock) {
							gen.AppendLine($".WithAll<{type.Name}TableExists>()");
							gen.AppendLine($".Build(ref state);");
						}

					}
					gen.AppendLine($"}}");

					gen.AppendLine($"");

					gen.AppendLine($"void ISystem.OnUpdate(ref SystemState state) {{");
					using(gen.IndentBlock) {
						gen.AppendLine($"EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);");

						gen.AppendLine($"");

						gen.AppendLine($"state.Dependency = new DestroyJob {{");
						using(gen.IndentBlock) {
							gen.AppendLine($"commandBuffer = commandBuffer,");
						}
						gen.AppendLine($"}}.Schedule(existsQuery, state.Dependency);");

						gen.AppendLine($"");

						gen.AppendLine($"state.Dependency = new DestroyJob {{");
						using(gen.IndentBlock) {
							gen.AppendLine($"commandBuffer = commandBuffer,");
						}
						gen.AppendLine($"}}.Schedule(requestQuery, state.Dependency);");
					}
					gen.AppendLine($"}}");

					gen.AppendLine($"");

					gen.AppendLine($"private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"return SystemAPI");
						using(gen.IndentBlock) {
							gen.AppendLine($".GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()");
							gen.AppendLine($".CreateCommandBuffer(state.World.Unmanaged);");
						}
					}
					gen.AppendLine($"}}");

					gen.AppendLine($"");
					gen.AppendLine($"partial struct DestroyJob : IJobEntity {{");
					using (gen.IndentBlock) {
						gen.AppendLine($"public EntityCommandBuffer commandBuffer;");
						gen.AppendLine($"");
						gen.AppendLine($"void Execute(in Entity entity) {{");
						using (gen.IndentBlock) {
							gen.AppendLine($"commandBuffer.DestroyEntity(entity);");
						}
						gen.AppendLine($"}}");
					}
					gen.AppendLine($"}}");
				}
				gen.AppendLine($"}}");

			}
			gen.AppendLine($"}}");

			gen.Generate($"dataTable{Path.DirectorySeparatorChar}systems{Path.DirectorySeparatorChar}{type.Name}DisposeSystem.cs", false);
		}
	}
}