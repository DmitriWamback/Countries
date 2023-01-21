import json
import matplotlib.pyplot as plt
import random
import math

# Geojson download: https://datahub.io/core/geo-countries#data-cli

content = None

with open('countries.geojson', 'r+') as file:

    content = file.read()
    content = json.loads(content)
    
length = len(content['features'])
fullGeometryData = {}
fullPolygonData  = {}

for i in range(length):
    countryName     = content['features'][i]['properties']['ADMIN']
    countryGeometry = content['features'][i]['geometry']
    polygonType = content['features'][i]['geometry']['type']

    fullGeometryData[countryName] = countryGeometry
    fullPolygonData[countryName]  = polygonType

countries = list(fullGeometryData.keys())

def get_country_geometry(country):

    test_geometry = list(fullGeometryData[country]['coordinates'])
    test_type     = fullPolygonData[country]

    geometry_x = []
    geometry_y = []

    if test_type == 'Polygon':
        for i in range(len(test_geometry[0])):
            geometry_x.append(test_geometry[0][i][0])
            geometry_y.append(test_geometry[0][i][1])
    else:
        for j in range(len(test_geometry)):
            for i in range(len(test_geometry[j])):
                for k in range(len(test_geometry[j][i])):
                    
                    longitude = test_geometry[j][i][k][0]
                    latitude  = test_geometry[j][i][k][1]

                    geometry_x.append(longitude)
                    geometry_y.append(latitude)

    return geometry_x, geometry_y


test_geometry_x = []
test_geometry_y = []

country_range = 150
chosen_countries = []

for i in range(len(fullGeometryData)):
    random_country = countries[i]
    chosen_countries.append(random_country)
    geometry_x, geometry_y = get_country_geometry(random_country)

    for x in geometry_x:
        test_geometry_x.append(x)

    for y in geometry_y:
        test_geometry_y.append(y)

'''
for i in range(country_range):
    random_country = countries[random.randint(0, country_range)]
    chosen_countries.append(random_country)
    geometry_x, geometry_y = get_country_geometry(random_country)

    for x in geometry_x:
        test_geometry_x.append(x)

    for y in geometry_y:
        test_geometry_y.append(y)
'''

plt.scatter(test_geometry_x, test_geometry_y, s=0.7, color='black')
#plt.plot(test_geometry_x, test_geometry_y)

plt.show()
print(chosen_countries)
