using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawingScript : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDrawing = false;
    private LineRenderer currentLineRenderer;
    private int currentIndex;
    private List<Vector3> linePositions = new List<Vector3>();
    public List<GameObject> objectsToAlign;

    public Material lineMaterial;
    public float lineWidth = 0.1f;
    public int smoothness = 10; // Количество точек для плавного рисования

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartDrawing(touch.position);
                    break;

                case TouchPhase.Moved:
                    if (isDrawing)
                        ContinueDrawing(touch.position);
                    break;

                case TouchPhase.Ended:
                    StopDrawing();
                    StartCoroutine(MoveObjectsSmoothly());
                    DestroyLine();
                    break;
            }
        }
    }

    void StartDrawing(Vector2 touchPosition)
    {
        Vector3 touchWorldPosition = GetWorldPosition(touchPosition);

        // Проверяем, что объект, по которому проводится палец, имеет тег "DrawPlane"
        if (touchWorldPosition != Vector3.zero && GetGameObject(touchPosition).tag == "DrawPlane")
        {
            GameObject lineObject = new GameObject("Line");
            lineObject.transform.SetParent(transform);
            currentLineRenderer = lineObject.AddComponent<LineRenderer>();
            currentLineRenderer.material = new Material(lineMaterial);
            currentLineRenderer.startWidth = lineWidth;
            currentLineRenderer.endWidth = lineWidth;
            currentLineRenderer.positionCount = 2; // Начальное количество точек
            currentIndex = 1;

            currentLineRenderer.SetPosition(0, touchWorldPosition);
            currentLineRenderer.SetPosition(1, touchWorldPosition);

            linePositions.Clear();
            linePositions.Add(touchWorldPosition);

            currentLineRenderer.useWorldSpace = false;
            isDrawing = true;
        }
    }

    void ContinueDrawing(Vector2 touchPosition)
    {
        if (isDrawing && currentLineRenderer != null)
        {
            Vector3 touchWorldPosition = GetWorldPosition(touchPosition);

            // Проверяем, что объект, по которому проводится палец, имеет тег "DrawPlane"
            if (touchWorldPosition != Vector3.zero && GetGameObject(touchPosition).tag == "DrawPlane")
            {
                currentIndex++;
                currentLineRenderer.positionCount = currentIndex + 1;

                // Используем кривую Безье для сглаживания линии
                currentLineRenderer.SetPosition(currentIndex, Vector3.Lerp(currentLineRenderer.GetPosition(currentIndex - 1), touchWorldPosition, 0.5f));

                // Обновляем следующую точку, если это не последняя точка
                if (currentIndex + 1 < currentLineRenderer.positionCount)
                {
                    currentLineRenderer.SetPosition(currentIndex + 1, touchWorldPosition);
                }

                linePositions.Add(touchWorldPosition);
            }
        }
    }

    void StopDrawing()
    {
        isDrawing = false;
    }

    IEnumerator MoveObjectsSmoothly()
    {
        if (linePositions.Count > 1 && objectsToAlign.Count > 0)
        {
            float moveDuration = 1.0f; // Длительность перемещения объектов
            List<Vector3> initialPositions = new List<Vector3>();

            // Сохраняем начальные позиции объектов
            foreach (GameObject obj in objectsToAlign)
            {
                initialPositions.Add(obj.transform.position);
            }

            float elapsedTime = 0f;

            while (elapsedTime < moveDuration)
            {
                float t = elapsedTime / moveDuration;

                for (int i = 0; i < objectsToAlign.Count; i++)
                {
                    float progress = (float)i / (objectsToAlign.Count - 1);
                    Vector3 targetPosition = BezierInterpolation(progress, linePositions.ToArray());

                    // Вычисляем относительную позицию объекта относительно его начальной позиции
                    Vector3 relativePosition = targetPosition - initialPositions[i];

                    // Проектируем относительную позицию на плоскость объекта
                    Vector3 projectedRelativePosition = Vector3.ProjectOnPlane(relativePosition, objectsToAlign[i].transform.up);

                    // Перемещаем объект относительно его начальной позиции, сохраняя плоскость
                    objectsToAlign[i].transform.position = initialPositions[i] + projectedRelativePosition * t;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }

    void DestroyLine()
    {
        if (currentLineRenderer != null)
        {
            Destroy(currentLineRenderer.gameObject);
            currentLineRenderer = null;
        }
    }

    // Метод для интерполяции по кривой Безье
    Vector3 BezierInterpolation(float t, Vector3[] points)
    {
        if (points.Length == 1)
            return points[0];

        Vector3[] newPoints = new Vector3[points.Length - 1];
        for (int i = 0; i < newPoints.Length; i++)
        {
            newPoints[i] = Vector3.Lerp(points[i], points[i + 1], t);
        }

        return BezierInterpolation(t, newPoints);
    }

    Vector3 GetWorldPosition(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    GameObject GetGameObject(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.transform.gameObject;
        }

        return null;
    }
}