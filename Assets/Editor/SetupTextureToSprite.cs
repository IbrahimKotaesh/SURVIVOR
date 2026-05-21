using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class SetupTextureToSprite
{
    [MenuItem("Tools/Convert Player Sprite")]
    public static void Convert()
    {
        string path = "Assets/player_eyeball_sprite.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Debug.Log("Converted " + path + " to Sprite successfully.");
        }
        else
        {
            Debug.LogError("Failed to find importer for: " + path);
        }
    }

    [MenuItem("Tools/Slice Van Dijk Sprite Sheet")]
    public static void SliceVanDijk()
    {
        string path = "Assets/Resources/grok_imagine_video_8aca20fa.png";
        
        // Ensure Resources folder exists
        if (!System.IO.Directory.Exists("Assets/Resources"))
        {
            System.IO.Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 200;

            // Define slices (8x8 grid, 63 frames)
            int columns = 8;
            int rows = 8;
            int frameWidth = 1280;
            int frameHeight = 704;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogError("Failed to load texture at path. Please make sure the file is placed at: " + path);
                return;
            }

            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            int frameIndex = 0;

            // Slicing top-to-bottom, left-to-right (Unity texture coordinates start bottom-left!)
            for (int r = rows - 1; r >= 0; r--)
            {
                for (int c = 0; c < columns; c++)
                {
                    if (frameIndex >= 63) break; // 63 frames total

                    SpriteMetaData meta = new SpriteMetaData();
                    meta.name = "grok_imagine_video_8aca20fa_" + frameIndex;
                    meta.rect = new Rect(c * frameWidth, r * frameHeight, frameWidth, frameHeight);
                    meta.alignment = (int)SpriteAlignment.BottomCenter;
                    meta.pivot = new Vector2(0.5f, 0f);
                    metas.Add(meta);
                    frameIndex++;
                }
            }

            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
            Debug.Log("Sliced Van Dijk Sprite Sheet successfully!");
        }
        else
        {
            Debug.LogError("Failed to find texture importer at: " + path + ". Make sure you have placed the PNG there!");
        }
    }

    [MenuItem("Tools/Setup Player Sprite Vergil 2")]
    public static void SetupPlayerVergil2()
    {
        string spritePath = "Assets/Resources/vergil_van_dijk_2.png";
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        Sprite firstFrame = null;
        foreach (var asset in subAssets)
        {
            if (asset is Sprite && asset.name.EndsWith("_0"))
            {
                firstFrame = (Sprite)asset;
                break;
            }
        }

        if (firstFrame == null)
        {
            Debug.LogError("Could not find the sliced sprites at " + spritePath + ". Please check if the file exists and is sliced.");
            return;
        }

        PlayerController player = GameObject.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Assign Vergil 2 Sprite");
                sr.sprite = firstFrame;
                Debug.Log("Assigned Vergil 2 first frame to Player SpriteRenderer.");
            }

            // Setup the animator
            SpriteSheetAnimator animator = player.GetComponent<SpriteSheetAnimator>();
            if (animator == null)
            {
                animator = player.gameObject.AddComponent<SpriteSheetAnimator>();
                Undo.RegisterCreatedObjectUndo(animator, "Add SpriteSheetAnimator");
                Debug.Log("Added SpriteSheetAnimator component to Player.");
            }

            // Update face flipping direction
            SerializedObject serializedPlayer = new SerializedObject(player);
            SerializedProperty facesRightProp = serializedPlayer.FindProperty("originalSpriteFacesRight");
            if (facesRightProp != null)
            {
                facesRightProp.boolValue = true;
                serializedPlayer.ApplyModifiedProperties();
                Debug.Log("Configured PlayerController to originalSpriteFacesRight = true.");
            }

            // Mark active scene as dirty to ensure changes save
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            }
            Debug.Log("Successfully configured Virgil van Dijk 2 character and animations on Player!");
        }
        else
        {
            Debug.LogWarning("PlayerController not found in the current active scene.");
        }
    }

    [MenuItem("Tools/Slice and Setup Van Dijk Player")]
    public static void SliceAndSetupPlayer()
    {
        // 1. First run the slicing function
        SliceVanDijk();

        // 2. Find the newly sliced sprite frames
        string spritePath = "Assets/Resources/grok_imagine_video_8aca20fa.png";
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        Sprite firstFrame = null;
        foreach (var asset in subAssets)
        {
            if (asset is Sprite && asset.name.EndsWith("_0"))
            {
                firstFrame = (Sprite)asset;
                break;
            }
        }

        if (firstFrame == null)
        {
            Debug.LogError("Could not find the sliced sprites. Please make sure grok_imagine_video_8aca20fa.png is inside Assets/Resources/ and that slicing completed without errors.");
            return;
        }

        // 3. Find Player in scene
        PlayerController player = GameObject.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            // Set first frame of Van Dijk
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Assign Van Dijk Sprite");
                sr.sprite = firstFrame;
                Debug.Log("Assigned Van Dijk first frame to Player SpriteRenderer.");
            }

            // Setup the animator
            SpriteSheetAnimator animator = player.GetComponent<SpriteSheetAnimator>();
            if (animator == null)
            {
                animator = player.gameObject.AddComponent<SpriteSheetAnimator>();
                Undo.RegisterCreatedObjectUndo(animator, "Add SpriteSheetAnimator");
                Debug.Log("Added SpriteSheetAnimator component to Player.");
            }

            // Update face flipping direction
            // Since Van Dijk original sprite sheet faces right, set originalSpriteFacesRight to true
            SerializedObject serializedPlayer = new SerializedObject(player);
            SerializedProperty facesRightProp = serializedPlayer.FindProperty("originalSpriteFacesRight");
            if (facesRightProp != null)
            {
                facesRightProp.boolValue = true;
                serializedPlayer.ApplyModifiedProperties();
                Debug.Log("Configured PlayerController to originalSpriteFacesRight = true.");
            }

            // Mark active scene as dirty to ensure changes save
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            }
            Debug.Log("Successfully configured Virgil van Dijk character and animations on Player!");
        }
        else
        {
            Debug.LogWarning("PlayerController not found in the current active scene. Make sure your main gameplay scene is open in the editor!");
        }
    }

    [MenuItem("Tools/Setup Infinite Background")]
    public static void SetupInfiniteBackground()
    {
        string path = "Assets/Resources/tileset_1779320312648.png";
        
        // 1. Configure texture importer for the tileset
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            
            // Set Wrap Mode to Repeat for seamless tiling
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 50; // Zoom in by setting smaller PPU (default is 100)
            
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // Prevents seam/edge trimming issues
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            Debug.Log("Configured Tileset Texture wrapMode to Repeat, PPU to 50, and MeshType to FullRect.");
        }
        else
        {
            Debug.LogError("Failed to find tileset texture at: " + path);
            return;
        }

        // Load the tileset sprite
        Sprite tilesetSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (tilesetSprite == null)
        {
            Debug.LogError("Failed to load tileset sprite from path: " + path);
            return;
        }

        // 2. Find any GameObject with InfiniteGround script
        InfiniteGround ground = GameObject.FindAnyObjectByType<InfiniteGround>();
        
        // If not found, look for objects named Ground, Tilemap, or Background
        if (ground == null)
        {
            GameObject groundObj = GameObject.Find("Ground");
            if (groundObj == null) groundObj = GameObject.Find("InfiniteGround");
            if (groundObj == null) groundObj = GameObject.Find("Background_Tiled");
            if (groundObj == null)
            {
                // Find any SpriteRenderer named "Background" or "Ground"
                SpriteRenderer[] srs = GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                foreach (var sr in srs)
                {
                    if (sr.gameObject.name.ToLower().Contains("ground") || sr.gameObject.name.ToLower().Contains("background"))
                    {
                        groundObj = sr.gameObject;
                        break;
                    }
                }
            }

            if (groundObj != null)
            {
                ground = groundObj.GetComponent<InfiniteGround>();
                if (ground == null)
                {
                    ground = groundObj.AddComponent<InfiniteGround>();
                    Undo.RegisterCreatedObjectUndo(ground, "Add InfiniteGround Script");
                }
            }
        }

        if (ground != null)
        {
            GameObject go = ground.gameObject;

            // Ensure SpriteRenderer is present
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = go.AddComponent<SpriteRenderer>();
                Undo.RegisterCreatedObjectUndo(sr, "Add SpriteRenderer for Ground");
            }

            Undo.RecordObject(sr, "Setup Infinite Ground Sprite");
            sr.sprite = tilesetSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            
            // Set size to cover a very large area so camera edges are never visible
            sr.size = new Vector2(60f, 60f);
            sr.sortingOrder = -10; // Ensure it renders behind everything

            // Configure snap dimensions based on sprite dimensions
            float sizeX = tilesetSprite.rect.width / tilesetSprite.pixelsPerUnit;
            float sizeY = tilesetSprite.rect.height / tilesetSprite.pixelsPerUnit;

            SerializedObject serializedGround = new SerializedObject(ground);
            SerializedProperty sizeXProp = serializedGround.FindProperty("tileSizeX");
            SerializedProperty sizeYProp = serializedGround.FindProperty("tileSizeY");

            if (sizeXProp != null) sizeXProp.floatValue = sizeX;
            if (sizeYProp != null) sizeYProp.floatValue = sizeY;
            serializedGround.ApplyModifiedProperties();

            // Set Z position to 1 (behind player)
            go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, 1f);

            // Mark active scene as dirty to ensure changes save
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            }

            Debug.Log($"Successfully configured Infinite Ground with tileset {tilesetSprite.name} (Tile Size: {sizeX}x{sizeY})!");
        }
        else
        {
            Debug.LogWarning("Could not find a Ground GameObject in the active scene to apply tileset. Please create a 2D GameObject named 'Ground' first!");
        }
    }

    [MenuItem("Tools/Log Ground Info")]
    public static void LogGroundInfo()
    {
        InfiniteGround ground = GameObject.FindAnyObjectByType<InfiniteGround>();
        if (ground != null)
        {
            string path = ground.gameObject.name;
            Transform t = ground.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            SerializedObject so = new SerializedObject(ground);
            float tx = so.FindProperty("tileSizeX").floatValue;
            float ty = so.FindProperty("tileSizeY").floatValue;
            
            SpriteRenderer sr = ground.GetComponent<SpriteRenderer>();
            string spriteName = sr != null && sr.sprite != null ? sr.sprite.name : "None";
            float ppu = sr != null && sr.sprite != null ? sr.sprite.pixelsPerUnit : 0;
            Vector2 size = sr != null ? sr.size : Vector2.zero;

            Debug.Log($"Ground Path: {path}, Parent: {(ground.transform.parent != null ? ground.transform.parent.name : "None")}\n" +
                      $"Position: {ground.transform.position}, Sprite: {spriteName}, PPU: {ppu}, DrawSize: {size}\n" +
                      $"Script TileSize: {tx}x{ty}");
        }
        else
        {
            Debug.LogWarning("No InfiniteGround found!");
        }
    }

    [MenuItem("Tools/Log Sprite Info")]
    public static void LogSpriteInfo()
    {
        string spritePath = "Assets/Resources/vergil_van_dijk_2.png";
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        Debug.Log("Total sub-assets found: " + subAssets.Length);
        int spriteCount = 0;
        foreach (var asset in subAssets)
        {
            if (asset is Sprite sprite)
            {
                spriteCount++;
                if (spriteCount <= 5 || spriteCount == subAssets.Length - 1)
                {
                    Debug.Log($"Sprite {sprite.name}: rect={sprite.rect}, pivot={sprite.pivot}, ppu={sprite.pixelsPerUnit}, bounds={sprite.bounds}");
                }
            }
        }
        Debug.Log("Total sprites: " + spriteCount);
    }

    [MenuItem("Tools/Configure Sprite Sheet PPU and Pivots")]
    public static void ConfigureSpriteSheets()
    {
        // 1. Configure Virgil
        ConfigureImporter("Assets/Resources/vergil_van_dijk_2.png", 1000f, 0.14f);
        // 2. Configure Vini
        ConfigureImporter("Assets/Resources/vini.png", 900f, 0.12f);
        
        AssetDatabase.Refresh();
        Debug.Log("ConfigureSpriteSheets: PPU and Pivots configured successfully!");
    }

    private static void ConfigureImporter(string path, float ppu, float pivotY)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("ConfigureImporter: Importer not found for path: " + path);
            return;
        }

        importer.spritePixelsPerUnit = ppu;
        
        if (importer.spriteImportMode == SpriteImportMode.Multiple)
        {
            var metas = importer.spritesheet;
            for (int i = 0; i < metas.Length; i++)
            {
                metas[i].alignment = (int)SpriteAlignment.Custom;
                metas[i].pivot = new Vector2(0.5f, pivotY);
            }
            importer.spritesheet = metas;
        }
        
        importer.SaveAndReimport();
        Debug.Log($"ConfigureImporter: Applied PPU={ppu}, pivotY={pivotY} to {path}");
    }
}


