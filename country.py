import json
import math

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

    polygons = []

    if test_type == 'Polygon':
        for i in range(len(test_geometry[0])):
            geometry_x.append(test_geometry[0][i][0])
            geometry_y.append(test_geometry[0][i][1])

        return geometry_x, geometry_y, False
    else:
        for j in range(len(test_geometry)):
            for i in range(len(test_geometry[j])):
                for k in range(len(test_geometry[j][i])):
                    
                    longitude = test_geometry[j][i][k][0]
                    latitude  = test_geometry[j][i][k][1]

                    geometry_x.append(longitude)
                    geometry_y.append(latitude)

                polygons.append(geometry_x)
                polygons.append(geometry_y)
                geometry_x = []
                geometry_y = []

    return polygons, None, True

def to_radians(p):
    return p * 3.14159265358797 / 180.0

def point_to_sphere(latitude, longitude):
    y = math.sin(to_radians(latitude))
    r = math.cos(to_radians(latitude))
    x = math.sin(to_radians(longitude)) * r
    z = -math.cos(to_radians(longitude)) * r

    return [x, y, z]
