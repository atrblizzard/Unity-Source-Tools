﻿using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace uSrcTools
{
	public class ResourceManager : MonoBehaviour
	{
		private static ResourceManager inst;
		public static ResourceManager Inst
		{
			get
			{
				return inst??(inst=new GameObject("ResourceManager").AddComponent<ResourceManager>());
			}
		}

		public Dictionary <string, SourceStudioModel> models 		= new Dictionary<string, SourceStudioModel>();
		public Dictionary <string, Texture> Textures 				= new Dictionary<string, Texture> ();
		public Dictionary <string, Material> Materials 				= new Dictionary<string, Material> ();
		public Dictionary <string, VMTLoader.VMTFile> VMTMaterials 	= new Dictionary<string, VMTLoader.VMTFile> ();

		void Awake()
		{
			inst = this;
		}

		public SourceStudioModel GetModel(string modelName)
		{
			modelName = modelName.ToLower ();
			if(models.ContainsKey(modelName))
			{
				return models[modelName];
			}
			else
			{
				SourceStudioModel tempModel = new SourceStudioModel().Load(modelName);
				models.Add (modelName, tempModel);
				return tempModel;
			}

		}

		public VMTLoader.VMTFile GetVMTMaterial(string materialName)
		{
			VMTLoader.VMTFile vmtFile=null;
			if (!VMTMaterials.ContainsKey (materialName)) 
			{
				vmtFile = VMTLoader.ParseVMTFile (materialName);
				VMTMaterials.Add (materialName, vmtFile);
			}
			else 
			{
				vmtFile = VMTMaterials [materialName];
			}
			return vmtFile;
		}

		public Texture GetTexture(string textureName)
		{
			textureName=textureName.ToLower();
			if(textureName.Contains("_rt_camera"))
			{
				if (!Textures.ContainsKey (textureName))
					Textures.Add (textureName,Test.Inst.cameraTexture);
				return Textures [textureName];
			}
			else
			{
				if (!Textures.ContainsKey (textureName))
					Textures.Add (textureName, VTFLoader.LoadFile (textureName));
				return Textures [textureName];
			}
		}

		public Material GetMaterial(string materialName)
		{
			Material tempmat=null;
			
			if(Materials.ContainsKey (materialName))
				return Materials[materialName];
			
			//VMT
			VMTLoader.VMTFile vmtFile = GetVMTMaterial (materialName);
			
			//Material
			if (vmtFile != null)
			{
				if(vmtFile.shader=="lightmappedgeneric")
				{
					if(vmtFile.selfillum)
						tempmat = new Material(uSrcSettings.Inst.resources.sSelfillum);
					else if(!vmtFile.translucent && !vmtFile.alphatest)
						//tempmat = new Material(uSrcSettings.Inst.resources.sDiffuse);
						tempmat = new Material(uSrcSettings.Inst.resources.diffuseMaterial);
					else
						//tempmat = new Material(uSrcSettings.Inst.resources.sTransparent); Also fixes the transparency.
						tempmat = new Material(uSrcSettings.Inst.resources.transparentMaterial);
				}
				else if(vmtFile.shader=="unlitgeneric")
				{
					if(vmtFile.additive)
					{
						tempmat = new Material(uSrcSettings.Inst.resources.sAdditive);
						tempmat.SetColor("_TintColor",Color.white);
					}
					else if(!vmtFile.translucent && !vmtFile.alphatest)
						tempmat = new Material(uSrcSettings.Inst.resources.sUnlit);
					else
						tempmat = new Material(uSrcSettings.Inst.resources.sUnlitTransparent);
				}
				else if(vmtFile.shader=="unlittwotexture")
				{
					tempmat = new Material(uSrcSettings.Inst.resources.sUnlit);
				}
				else if(vmtFile.shader=="vertexlitgeneric")
				{
					if(vmtFile.selfillum)
						tempmat = new Material(uSrcSettings.Inst.resources.sSelfillum);
					else if(vmtFile.alphatest)
						//tempmat = new Material(uSrcSettings.Inst.resources.sAlphatest); Slow but sure move to materials instead of shaders.
						tempmat = new Material(uSrcSettings.Inst.resources.transparentCutout);
					else if(vmtFile.translucent) 
						//tempmat = new Material(uSrcSettings.Inst.resources.sTransparent); This fixes the transparency.
						tempmat = new Material(uSrcSettings.Inst.resources.transparentMaterial);
					else
						//tempmat = new Material(uSrcSettings.Inst.resources.sVertexLit); This is to turn down the smoothness.
						tempmat = new Material(uSrcSettings.Inst.resources.vertexLitMaterial);
				}
				else if(vmtFile.shader=="refract")
				{
					tempmat = new Material(uSrcSettings.Inst.resources.sRefract);
				}
				else if(vmtFile.shader=="worldvertextransition")
				{
					tempmat = new Material(uSrcSettings.Inst.resources.sWorldVertexTransition);

					string bt2=vmtFile.basetexture2;
					Texture tex2=GetTexture(bt2);
					tempmat.SetTexture("_MainTex2",tex2);
					if(tex2==null)
						Debug.LogWarning("Error loading second texture "+bt2+" from material "+materialName);
				}
				else if(vmtFile.shader=="water")
				{
					Debug.LogWarning("Shader "+vmtFile.shader+" from VMT "+materialName+" not suported");
					tempmat = new Material(uSrcSettings.Inst.resources.transparentMaterial);
					tempmat.color=new Color(1,1,1,0.3f);
				}
				else if(vmtFile.shader=="black")
				{
					tempmat = new Material(uSrcSettings.Inst.resources.sUnlit);
					tempmat.color=Color.black;
				}
				else if(vmtFile.shader=="infected")
				{
					tempmat = new Material(uSrcSettings.Inst.resources.diffuseMaterial);
				}
				/*else if(vmtFile.shader=="eyerefract")
				{
					Debug.Log ("EyeRefract shader not done. Used Diffuse");
					tempmat = new Material(uSrcSettings.Inst.resources.sDiffuse);
				}*/
				else
				{
					Debug.LogWarning("Shader "+vmtFile.shader+" from VMT "+materialName+" not suported");
					tempmat = new Material(uSrcSettings.Inst.resources.diffuseMaterial);
				}
				
				tempmat.name = materialName;
				
				string textureName = vmtFile.basetexture;

				if(textureName!=null)
				{
					textureName = textureName.ToLower();

					Texture mainTex=GetTexture(textureName);
					tempmat.mainTexture = mainTex;

                    // Replace name
                    tempmat.name = tempmat.name.Replace("maps/" + Test.Inst.mapName + "/", "");

                    if (mainTex == null)
						Debug.LogWarning("Error loading texture "+textureName+" from material "+materialName);
				}
				else
				{
					//tempmat.shader = Shader.Find ("Transparent/Diffuse");
					//tempmat.color = new Color (1, 1, 1, 0f);
				}
				
				if(vmtFile.dudvmap!=null&vmtFile.shader=="refract")
				{
					string dudv=vmtFile.dudvmap.ToLower ();
					Texture dudvTex=GetTexture(dudv);
					tempmat.SetTexture("_BumpMap",dudvTex);
					if(dudvTex==null)
						Debug.LogWarning("Error loading texture "+dudv+" from material "+materialName);
				}
					
				Materials.Add (materialName,tempmat);
				return tempmat;
			}
			else
			{
				//Debug.LogWarning("Error loading "+materialName);
				Materials.Add (materialName, Test.Inst.testMaterial);
				return Test.Inst.testMaterial;
			}
		}

		public static string GetPath(string filename)
		{
			filename=filename.Replace ("\\","/");
			filename=filename.Replace ("//","/");
			string path = string.Empty;

			if(uSrcSettings.Inst.haveMod)
			{
				if(uSrcSettings.Inst.mod!="none"||uSrcSettings.Inst.mod!="")
				{
					path = uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.mod + "/";
					if(CheckFile(path + filename))
					{
						return path + filename;
					}
				}
			}
			
			path = uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/";

            string renamedFile = filename;
            if (filename.Contains(".vmt") || filename.Contains(".vtf"))
                renamedFile = ProcessTextureNames(filename);

            if (CheckFile(path + renamedFile))
                return path + renamedFile;
            else if (CheckFullFiles(renamedFile))
                return path + renamedFile;

            Debug.LogWarning (uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/" + filename+": Not Found");
			return null;
		}
	
		static bool CheckFullFiles(string filename)
		{
			string path=uSrcSettings.Inst.path + "/"+uSrcSettings.Inst.game+"full/";
			
			if(!CheckFile(path + filename))
				return false;
			
			string dirpath=uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/";
			
			Debug.LogWarning ("Copying: "+path + filename+" to "+dirpath+filename);

			
			if(!Directory.Exists (dirpath + filename.Remove(filename.LastIndexOf("/"))))
				Directory.CreateDirectory(dirpath + filename.Remove(filename.LastIndexOf("/")));
				
			File.Copy ( path + filename, dirpath+ filename);
		
			return true;
		
		}
	
		public static string FindModelMaterialFile(string filename, string[] dirs)
		{
			filename=filename.Replace ("\\","/");
			filename=filename.Replace ("//","/");
			filename+=".vmt";
			string path="";
			
			path = uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/materials/";
			if(CheckFile(path + filename))
			{
				return filename;
			}
			else if(CheckFullFiles("materials/"+filename))
			{
				return filename;
			}
			
			for(int i=0;i<dirs.Length;i++)
			{
				path = uSrcSettings.Inst.path + "/" + uSrcSettings.Inst.game + "/materials/"+dirs[i];
				if(CheckFile(path + filename))
				{
					return dirs[i] + filename;
				}
				else if(CheckFullFiles("materials/"+dirs[i]+filename))
				{
					return dirs[i] + filename;
				}
			}
			
			Debug.LogWarning ("Model material "+dirs[0]+filename+": Not Found");
			return dirs[0]+filename;
		}
	
		static bool CheckFile(string path)
		{
			if(Directory.Exists (path.Remove(path.LastIndexOf("/"))))
			{
				if(File.Exists (path))
				{
					return true;
				}
			}
			return false;
		}

        /*
		* Code taken from BSPSource by Nico Bergemann (ata4)
        * Converts environment-mapped texture names to original texture names and
        * performs some cleanups. It also assigns cubemap IDs to texname IDs which
        * is later used to create the "sides" properties of env_cubemap entities.
        * 
        * Following operations will be performed:
        * - chop "maps/mapname/" from start of names
        * - chop "_n_n_n or _n_n_n_depth_n from end of names
        */
        public static string ProcessTextureNames(string filename)
        {
            string regex = @"_-?(\d+)_?-?(\d+)_?-?(\d+)";

            // search for "maps/<mapname>" prefix
            string textureNew = filename.Replace("maps/" + Test.Inst.mapName + "/", string.Empty);

            // search and replace origin coordinates
            textureNew = Regex.Replace(textureNew, regex, string.Empty);

            // search for and replace "_depth_xxx" suffix
            textureNew = Regex.Replace(textureNew, "_depth_(-?\\d+)", string.Empty);

            // search for and replace _wvt_patch" suffix
            textureNew = Regex.Replace(textureNew, "_wvt_patch", string.Empty);

            return textureNew;
        }
    }
}