using System.Collections.Generic;
using UnityEngine;

namespace TopDownRacing
{
    /// <summary>
    /// Бесконечный мир: трава под дорогой и сама дорога.
    /// Тайлы переиспользуются, поэтому трасса никогда не заканчивается.
    /// </summary>
    public class InfiniteGround : MonoBehaviour
    {
        [Header("Трава")]
        public Sprite[] grassTiles;
        public float grassTileSize = 2f;
        public int gridColumns = 16;
        public int gridRows = 12;
        public int grassSortingOrder = -20;

        [Header("Дорога")]
        public Sprite roadSprite;
        public float roadWidth = 8f;
        public float roadSegmentHeight = 8f;
        public int roadSegmentCount = 7;
        public float roadX = 0f;
        public int roadSortingOrder = -10;

        private SpriteRenderer[] grassRenderers;
        private SpriteRenderer[] roadRenderers;
        private Vector2Int lastBaseCell;
        private bool initialized;
        private Transform cameraTransform;

        private void Start()
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
            AdoptExisting();
            BuildGrass();
            BuildRoad();
            initialized = true;
        }

        /// <summary>Подхватывает объекты, созданные редактором, чтобы сцена была видна и вне Play Mode.</summary>
        private void AdoptExisting()
        {
            List<SpriteRenderer> grass = new List<SpriteRenderer>();
            List<SpriteRenderer> road = new List<SpriteRenderer>();

            foreach (Transform child in transform)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    continue;
                }

                if (child.name.StartsWith("Road"))
                {
                    road.Add(renderer);
                }
                else
                {
                    grass.Add(renderer);
                }
            }

            if (grass.Count > 0)
            {
                grassRenderers = grass.ToArray();
            }

            if (road.Count > 0)
            {
                roadRenderers = road.ToArray();
            }
        }

        private void BuildGrass()
        {
            if (grassRenderers != null && grassRenderers.Length > 0)
            {
                return;
            }

            if (grassTiles == null || grassTiles.Length == 0)
            {
                return;
            }

            int count = gridColumns * gridRows;
            grassRenderers = new SpriteRenderer[count];

            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("Grass_" + i);
                go.transform.SetParent(transform, false);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = grassTiles[0];
                renderer.sortingOrder = grassSortingOrder;
                grassRenderers[i] = renderer;
            }
        }

        private void BuildRoad()
        {
            if (roadRenderers != null && roadRenderers.Length > 0)
            {
                return;
            }

            if (roadSprite == null)
            {
                return;
            }

            roadRenderers = new SpriteRenderer[roadSegmentCount];

            for (int i = 0; i < roadSegmentCount; i++)
            {
                GameObject go = new GameObject("RoadSegment_" + i);
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(roadX, (i - roadSegmentCount / 2) * roadSegmentHeight, 0f);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = roadSprite;
                renderer.sortingOrder = roadSortingOrder;
                roadRenderers[i] = renderer;
            }
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            if (cameraTransform == null)
            {
                cameraTransform = Camera.main != null ? Camera.main.transform : null;
                if (cameraTransform == null)
                {
                    return;
                }
            }

            UpdateGrass();
            UpdateRoad();
        }

        private void UpdateGrass()
        {
            if (grassRenderers == null || grassRenderers.Length == 0)
            {
                return;
            }

            Vector3 camPos = cameraTransform.position;
            Vector2Int baseCell = new Vector2Int(
                Mathf.FloorToInt(camPos.x / grassTileSize),
                Mathf.FloorToInt(camPos.y / grassTileSize));

            if (baseCell == lastBaseCell)
            {
                return;
            }

            lastBaseCell = baseCell;

            int index = 0;
            for (int j = 0; j < gridRows; j++)
            {
                for (int i = 0; i < gridColumns; i++)
                {
                    if (index >= grassRenderers.Length)
                    {
                        return;
                    }

                    Vector2Int cell = new Vector2Int(
                        baseCell.x + i - gridColumns / 2,
                        baseCell.y + j - gridRows / 2);

                    SpriteRenderer renderer = grassRenderers[index++];
                    renderer.transform.position = new Vector3(
                        (cell.x + 0.5f) * grassTileSize,
                        (cell.y + 0.5f) * grassTileSize,
                        0f);
                    renderer.sprite = grassTiles[VariantIndex(cell)];
                }
            }
        }

        private int VariantIndex(Vector2Int cell)
        {
            unchecked
            {
                int hash = cell.x * 73856093 ^ cell.y * 19349663;
                hash = (hash ^ (hash >> 13)) * 1274126177;
                return Mathf.Abs(hash) % grassTiles.Length;
            }
        }

        private void UpdateRoad()
        {
            if (roadRenderers == null || roadRenderers.Length == 0)
            {
                return;
            }

            float poolHeight = roadSegmentCount * roadSegmentHeight;

            // Границы видимой области камеры.
            Camera camera = cameraTransform.GetComponent<Camera>();
            float halfHeight = camera != null && camera.orthographic ? camera.orthographicSize : 6f;
            float camBottom = cameraTransform.position.y - halfHeight;
            float camTop = cameraTransform.position.y + halfHeight;

            // Сегменты должны переиспользоваться в обе стороны: камера может и ускоряться,
            // и тормозить (после удара или на финише), иначе края дороги уезжают с экрана.
            foreach (SpriteRenderer renderer in roadRenderers)
            {
                float bottom = renderer.transform.position.y - roadSegmentHeight * 0.5f;
                float top = renderer.transform.position.y + roadSegmentHeight * 0.5f;

                if (bottom < camBottom - roadSegmentHeight)
                {
                    renderer.transform.position += Vector3.up * poolHeight;
                }
                else if (top > camTop + roadSegmentHeight)
                {
                    renderer.transform.position -= Vector3.up * poolHeight;
                }
            }
        }
    }
}
