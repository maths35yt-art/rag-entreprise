# Évaluation du RAG

Date : 25/11/2026 09:19. Service évalué : http://localhost:8000/ask.

Taux de réponses correctes : 8 sur 8 (100 %).

Passage utile présent parmi les passages récupérés : 7 sur 8. Questions ayant déclenché un outil externe : 0 sur 8. Temps de réponse moyen : 98.5 s.

Erreurs par cause : récupération 0, génération 0, service 0.

## Tableau des erreurs

Aucune erreur sur ce jeu de questions.

## Méthode

Une réponse est jugée correcte si elle contient tous les éléments attendus, la comparaison ignorant la casse, les accents et la ponctuation. Cette méthode peut rejeter une bonne réponse formulée avec d'autres mots, c'est pourquoi la réponse obtenue figure dans le tableau pour relecture. Une erreur est attribuée à la récupération quand les éléments attendus sont absents des passages fournis au modèle, à la génération quand ils étaient présents mais n'apparaissent pas dans la réponse, et au service quand la requête a échoué.
