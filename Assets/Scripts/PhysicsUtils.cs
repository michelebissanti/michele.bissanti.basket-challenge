using UnityEngine;

public static class PhysicsUtils
{
    /// <summary>
    /// Calcola la velocità iniziale (forza) per lanciare un proiettile
    /// dal punto di partenza al punto di destinazione, dato un angolo di lancio.
    /// </summary>
    /// <param name="start">Posizione di partenza</param>
    /// <param name="target">Posizione di destinazione</param>
    /// <param name="angleInDegrees">Angolo di lancio in gradi (rispetto all'orizzonte)</param>
    /// <param name="foundSolution">Restituisce true se è stata trovata una soluzione, false altrimenti.</param>
    /// <returns>Il vettore velocità iniziale per colpire il bersaglio.</returns>
    public static Vector3 CalculatePerfectShotVelocity(Vector3 start, Vector3 target, float angleInDegrees, out bool foundSolution)
    {
        // Ottieni la gravità (è un valore negativo, es. -9.81)
        float g = Physics.gravity.y;

        // --- 1. Separare la distanza in Orizzontale (xz) e Verticale (y) ---

        // Vettore di spostamento totale
        Vector3 delta = target - start;

        // Spostamento sul piano orizzontale (xz)
        Vector3 deltaXZ = new Vector3(delta.x, 0, delta.z);

        // Distanza orizzontale (magnitudo)
        float x = deltaXZ.magnitude;

        // Spostamento verticale (differenza di altezza)
        float y = delta.y;

        // --- 2. Calcolare la velocità usando la formula del moto parabolico ---

        // Converti l'angolo in radianti
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // Calcola seno e tangente dell'angolo
        float tanTheta = Mathf.Tan(angleInRadians);

        // Questa è la formula risolta per la velocità orizzontale (vH) al quadrato.
        // vH^2 = (0.5 * g * x^2) / (y - x * tan(theta))
        // La derivazione completa è complessa, ma questa è la soluzione.

        float numerator = 0.5f * g * x * x;
        float denominator = y - x * tanTheta;

        // Poiché g è negativo, anche il numeratore è negativo.
        // Affinché la velocità^2 sia positiva, anche il denominatore deve essere negativo.
        // Se il denominatore è >= 0, il bersaglio è irraggiungibile con questo angolo
        // (es. è "troppo in alto" o "dietro" la parabola).

        if (denominator >= 0)
        {
            foundSolution = false;
            return Vector3.zero;
        }

        float vH_squared = numerator / denominator;

        // Ora abbiamo la velocità orizzontale (non al quadrato)
        float vH = Mathf.Sqrt(vH_squared);

        // La velocità verticale iniziale è vH * tan(theta)
        float vY = vH * tanTheta;

        // --- 3. Combinare le velocità in un Vettore 3D ---

        // Direzione orizzontale (un vettore lungo 1 che punta verso il target)
        Vector3 directionXZ = deltaXZ.normalized;

        // Vettore velocità orizzontale
        Vector3 velocityXZ = directionXZ * vH;

        // Vettore velocità verticale
        Vector3 velocityY = Vector3.up * vY;

        foundSolution = true;

        // La velocità iniziale totale è la somma delle componenti
        return velocityXZ + velocityY;
    }
}